using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using AdminPlatform.Modules.Customer.Application.Auth;
using AdminPlatform.Modules.Notification.Application.Otp;
using AdminPlatform.Modules.Notification.Application.Ports;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace AdminPlatform.IntegrationTests;

/// <summary>The sign-up flow with phone verification switched ON. Verification is turned on only for these tests (by
/// overriding the options of a derived host), so every other test keeps registering without a code. A capturing sender
/// replaces the real provider so the test can read the code that would have been sent.</summary>
[Collection("Api")]
public class CustomerPhoneVerificationFlowTests
{
    private sealed class CapturingSender : IMessageSender
    {
        public ConcurrentQueue<OutboundMessage> Sent { get; } = new();

        public Task<MessageSendResult> SendAsync(OutboundMessage message, CancellationToken cancellationToken)
        {
            Sent.Enqueue(message);
            return Task.FromResult(new MessageSendResult(true, "test", "test", "id", "100"));
        }
    }

    private readonly AdminPlatformApiFactory _factory;

    public CustomerPhoneVerificationFlowTests(AdminPlatformApiFactory factory)
    {
        _factory = factory;
    }

    private WebApplicationFactory<Program> WithVerification(CapturingSender sender) =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.Configure<PhoneVerificationOptions>(options => options.Required = true);
            services.Configure<OtpOptions>(options =>
            {
                options.HmacKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                options.ResendCooldownSeconds = 0;
                options.MaxPerPhonePerHour = 1000;
                options.MaxPerIpPerHour = 1000;
                options.DailyCap = 1_000_000;
            });
            services.AddSingleton<IMessageSender>(sender);
        }));

    private static string NewPhone() => "09" + RandomNumberGenerator.GetInt32(0, 100_000_000).ToString("D8");

    private static string NewEmail() => $"otp-{Guid.NewGuid():n}@integration.test";

    [Fact]
    public async Task Registering_needs_a_code_and_a_right_code_creates_a_verified_account_once()
    {
        var sender = new CapturingSender();
        using var factory = WithVerification(sender);
        using var client = factory.CreateClient();
        var phone = NewPhone();
        var email = NewEmail();

        // No code at all: refused.
        var withoutCode = await client.PostAsJsonAsync("/api/v1/customer/auth/register", new RegisterCustomerRequest("Jane", phone, email, "S3curePassw0rd!"));
        Assert.Equal(HttpStatusCode.BadRequest, withoutCode.StatusCode);

        // Step 1: ask for a code.
        var otpResponse = await client.PostAsJsonAsync("/api/v1/customer/auth/register/otp", new RequestRegistrationOtpRequest(phone, email));
        Assert.Equal(HttpStatusCode.OK, otpResponse.StatusCode);
        var issued = (await otpResponse.Content.ReadFromJsonAsync<RegistrationOtpResponse>())!;
        var message = Assert.Single(sender.Sent);
        Assert.Equal(phone, message.Phone);
        var code = message.Parameters["code"];

        // A wrong code is refused (and counts), the right one creates the account.
        var wrong = code == "000000" ? "111111" : "000000";
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/v1/customer/auth/register", new RegisterCustomerRequest("Jane", phone, email, "S3curePassw0rd!", issued.ChallengeId, wrong))).StatusCode);

        var registered = await client.PostAsJsonAsync("/api/v1/customer/auth/register", new RegisterCustomerRequest("Jane", phone, email, "S3curePassw0rd!", issued.ChallengeId, code));
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var tokens = (await registered.Content.ReadFromJsonAsync<CustomerTokenResponse>())!;

        using var me = factory.CreateClient();
        me.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        var profile = (await me.GetFromJsonAsync<CustomerMeResponse>("/api/v1/customers/me"))!;
        Assert.Equal(phone, profile.Phone);
        Assert.True(profile.PhoneVerified);
    }

    [Fact]
    public async Task A_code_works_only_once_and_a_taken_email_is_reported_before_any_code_is_sent()
    {
        var sender = new CapturingSender();
        using var factory = WithVerification(sender);
        using var client = factory.CreateClient();
        var phone = NewPhone();
        var email = NewEmail();

        var issued = (await (await client.PostAsJsonAsync("/api/v1/customer/auth/register/otp", new RequestRegistrationOtpRequest(phone, email)))
            .Content.ReadFromJsonAsync<RegistrationOtpResponse>())!;
        var code = sender.Sent.Single().Parameters["code"];
        Assert.Equal(HttpStatusCode.Created,
            (await client.PostAsJsonAsync("/api/v1/customer/auth/register", new RegisterCustomerRequest("Jane", phone, email, "S3curePassw0rd!", issued.ChallengeId, code))).StatusCode);

        // The same code cannot make a second account.
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.PostAsJsonAsync("/api/v1/customer/auth/register", new RegisterCustomerRequest("Jane", NewPhone(), NewEmail(), "S3curePassw0rd!", issued.ChallengeId, code))).StatusCode);

        // Asking for a code for the registered email or phone is a 409 and sends nothing.
        var sentBefore = sender.Sent.Count;
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync("/api/v1/customer/auth/register/otp", new RequestRegistrationOtpRequest(NewPhone(), email))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync("/api/v1/customer/auth/register/otp", new RequestRegistrationOtpRequest(phone, NewEmail()))).StatusCode);
        Assert.Equal(sentBefore, sender.Sent.Count);
    }

    [Fact]
    public async Task A_phone_that_is_not_vietnamese_is_refused_with_a_validation_error()
    {
        using var factory = WithVerification(new CapturingSender());
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/customer/auth/register/otp", new RequestRegistrationOtpRequest("12345", NewEmail()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task With_verification_off_the_otp_endpoint_is_refused_and_registering_needs_no_code()
    {
        using var client = _factory.CreateClient();

        var otp = await client.PostAsJsonAsync("/api/v1/customer/auth/register/otp", new RequestRegistrationOtpRequest(NewPhone(), NewEmail()));
        Assert.Equal(HttpStatusCode.BadRequest, otp.StatusCode);

        var registered = await client.PostAsJsonAsync("/api/v1/customer/auth/register", new RegisterCustomerRequest("Jane", NewPhone(), NewEmail(), "S3curePassw0rd!"));
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
    }
}

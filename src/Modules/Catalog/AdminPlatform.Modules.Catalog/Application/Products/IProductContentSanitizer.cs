namespace AdminPlatform.Modules.Catalog.Application.Products;

/// <summary>Strips anything executable from the product's rich-text detail before it is stored, because
/// the public website renders it as raw HTML. Implemented in Infrastructure.</summary>
public interface IProductContentSanitizer
{
    string Sanitize(string html);
}

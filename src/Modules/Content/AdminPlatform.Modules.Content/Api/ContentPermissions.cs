namespace AdminPlatform.Modules.Content.Api;

public static class ContentPermissions
{
    public const string ArticlesView = "articles.view";
    public const string ArticlesCreate = "articles.create";
    public const string ArticlesUpdate = "articles.update";
    public const string ArticlesDelete = "articles.delete";

    public const string ArticleCategoriesView = "article-categories.view";
    public const string ArticleCategoriesCreate = "article-categories.create";
    public const string ArticleCategoriesUpdate = "article-categories.update";
    public const string ArticleCategoriesDelete = "article-categories.delete";

    public const string ArticleTagsView = "article-tags.view";
    public const string ArticleTagsCreate = "article-tags.create";
    public const string ArticleTagsUpdate = "article-tags.update";
    public const string ArticleTagsDelete = "article-tags.delete";

    public const string BannersView = "banners.view";
    public const string BannersCreate = "banners.create";
    public const string BannersUpdate = "banners.update";
    public const string BannersDelete = "banners.delete";

    /// <summary>Also cover the sections of a page.</summary>
    public const string PagesView = "pages.view";
    public const string PagesCreate = "pages.create";
    public const string PagesUpdate = "pages.update";
    public const string PagesDelete = "pages.delete";

    public static IReadOnlyList<(string Code, string Description)> All { get; } =
    [
        (ArticlesView, "View articles"),
        (ArticlesCreate, "Create articles"),
        (ArticlesUpdate, "Update articles"),
        (ArticlesDelete, "Delete articles"),
        (ArticleCategoriesView, "View article categories"),
        (ArticleCategoriesCreate, "Create article categories"),
        (ArticleCategoriesUpdate, "Update article categories"),
        (ArticleCategoriesDelete, "Delete article categories"),
        (ArticleTagsView, "View article tags"),
        (ArticleTagsCreate, "Create article tags"),
        (ArticleTagsUpdate, "Update article tags"),
        (ArticleTagsDelete, "Delete article tags"),
        (BannersView, "View banners"),
        (BannersCreate, "Create banners"),
        (BannersUpdate, "Update banners"),
        (BannersDelete, "Delete banners"),
        (PagesView, "View pages and their sections"),
        (PagesCreate, "Create pages and sections"),
        (PagesUpdate, "Update pages and sections"),
        (PagesDelete, "Delete pages and sections"),
    ];
}

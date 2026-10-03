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
    ];
}

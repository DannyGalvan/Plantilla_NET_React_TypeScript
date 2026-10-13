namespace Project.Modules.Files.Contracts;

/// <summary>
/// Permission keys of the Files module. Attachments of a business record use the permissions of that record
/// (declared with <c>AddAttachmentTarget</c>), not these ones.
/// </summary>
public static class FilesPermissions
{
    public const string Module = "files";

    /// <summary>Files not attached to any record (shared documents of the tenant).</summary>
    public static class Library
    {
        public const string Read = "files.library.read";
        public const string Upload = "files.library.upload";
        public const string Delete = "files.library.delete";
    }

    /// <summary>Connect Google Drive, OneDrive, S3 or Azure Blob and choose the default storage.</summary>
    public static class Storage
    {
        public const string Manage = "files.storage.manage";
    }
}

/// <summary>Names of the storage providers.</summary>
public static class StorageProviders
{
    public const string Local = "local";
    public const string S3 = "s3";
    public const string AzureBlob = "azure-blob";
    public const string GoogleDrive = "google-drive";
    public const string OneDrive = "onedrive";
}

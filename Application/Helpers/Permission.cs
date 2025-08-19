namespace Application
{
    public class Permission
    {
        public int PermissionId { get; set; } // PermissionID
        public string RoleName { get; set; } // RoleName (length: 100)
        public string PermissionName { get; set; } // FormName (length: 50)
        public string Access { get; set; } // Access (length: 50)
        public string PermissionType { get; set; } // PermissionType (length: 4)

    }
}

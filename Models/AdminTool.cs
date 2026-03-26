namespace AdminToolbox.Models;

/// <summary>Defines one RSAT MMC snap-in entry shown on the toolbox dashboard.</summary>
public sealed record AdminTool(
    string Name,
    string Description,
    string MscPath,
    string IconGlyph)
{
    /// <summary>All known admin/RSAT tools. Some may not be installed on the current system.</summary>
    public static readonly IReadOnlyList<AdminTool> Known = new List<AdminTool>
    {
        // ---- RSAT Role Administration Tools ----
        new("AD Users",          "Active Directory Users & Computers",  @"%SystemRoot%\system32\dsa.msc",               "\uE716"),
        new("AD Domains",        "Active Directory Domains and Trusts", @"%SystemRoot%\system32\domain.msc",            "\uE71B"),
        new("AD Sites",          "Active Directory Sites and Services", @"%SystemRoot%\system32\dssite.msc",            "\uE774"),
        new("DHCP",              "DHCP Server Management",             @"%SystemRoot%\system32\dhcpmgmt.msc",          "\uE968"),
        new("DNS",               "DNS Server Management",              @"%SystemRoot%\system32\dnsmgmt.msc",           "\uE774"),
        new("GPO",               "Group Policy Management",            @"%SystemRoot%\system32\gpmc.msc",              "\uE83D"),
        new("Print Mgmt",       "Print Management",                   @"%SystemRoot%\system32\printmanagement.msc",   "\uE749"),
        new("DFS Mgmt",         "DFS Management",                     @"%SystemRoot%\system32\dfsmgmt.msc",           "\uED25"),
        new("Cert Authority",   "Certification Authority",             @"%SystemRoot%\system32\certsrv.msc",           "\uE72E"),
        new("Cert Templates",   "Certificate Templates",               @"%SystemRoot%\system32\certtmpl.msc",          "\uE8D7"),

        // ---- Built-in Windows Admin Tools ----
        new("Computer Mgmt",    "Computer Management",                 @"%SystemRoot%\system32\compmgmt.msc",          "\uE7F4"),
        new("Event Viewer",     "Windows Event Viewer",                @"%SystemRoot%\system32\eventvwr.msc",          "\uE783"),
        new("Services",         "Windows Services",                    @"%SystemRoot%\system32\services.msc",          "\uE912"),
        new("Task Scheduler",   "Task Scheduler",                      @"%SystemRoot%\system32\taskschd.msc",          "\uE823"),
        new("Disk Mgmt",        "Disk Management",                     @"%SystemRoot%\system32\diskmgmt.msc",          "\uEDA2"),
        new("Device Mgr",       "Device Manager",                      @"%SystemRoot%\system32\devmgmt.msc",           "\uE772"),
        new("Shared Folders",   "Shared Folders",                      @"%SystemRoot%\system32\fsmgmt.msc",            "\uE8B7"),
        new("Local Users",      "Local Users and Groups",              @"%SystemRoot%\system32\lusrmgr.msc",           "\uE77B"),
        new("Firewall",         "Windows Firewall (Advanced)",         @"%SystemRoot%\system32\wf.msc",                "\uE83D"),
        new("Local GPO",        "Local Group Policy Editor",           @"%SystemRoot%\system32\gpedit.msc",            "\uE8C8"),
        new("Security Policy",  "Local Security Policy",               @"%SystemRoot%\system32\secpol.msc",            "\uE72E"),
    };

    /// <summary>Returns only the tools whose .msc file exists on this machine.</summary>
    public static List<AdminTool> GetInstalled()
    {
        var result = new List<AdminTool>();
        foreach (var tool in Known)
        {
            var expanded = Environment.ExpandEnvironmentVariables(tool.MscPath);
            if (System.IO.File.Exists(expanded))
                result.Add(tool);
        }
        return result;
    }
}

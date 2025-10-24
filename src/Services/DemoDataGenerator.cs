namespace QRStickers.Services;

/// <summary>
/// Generates realistic demo/fake data for demo Meraki connections
/// Used for marketing screenshots and demonstrations
/// </summary>
public static class DemoDataGenerator
{
    /// <summary>
    /// Generates demo organizations
    /// </summary>
    public static List<Organization> GenerateOrganizations()
    {
        return new List<Organization>
        {
            new Organization
            {
                Id = "demo-org-1",
                Name = "Acme Corp - Headquarters",
                Url = "https://n123.meraki.com/o/demo1/manage/organization/overview"
            },
            new Organization
            {
                Id = "demo-org-2",
                Name = "Acme Corp - Retail Division",
                Url = "https://n123.meraki.com/o/demo2/manage/organization/overview"
            }
        };
    }

    /// <summary>
    /// Generates demo networks for a given organization ID
    /// </summary>
    public static List<Network> GenerateNetworks(string organizationId)
    {
        return organizationId switch
        {
            "demo-org-1" => new List<Network>
            {
                new Network
                {
                    Id = "demo-net-1",
                    OrganizationId = organizationId,
                    Name = "Corporate Campus",
                    ProductTypes = new List<string> { "wireless", "switch", "appliance", "camera" },
                    Tags = new List<string> { "production", "corporate" },
                    TimeZone = "America/Los_Angeles",
                    Url = "https://n123.meraki.com/demo-net-1/n/manage/nodes/list"
                },
                new Network
                {
                    Id = "demo-net-2",
                    OrganizationId = organizationId,
                    Name = "Guest WiFi Network",
                    ProductTypes = new List<string> { "wireless" },
                    Tags = new List<string> { "guest", "isolated" },
                    TimeZone = "America/Los_Angeles",
                    Url = "https://n123.meraki.com/demo-net-2/n/manage/nodes/list"
                }
            },
            "demo-org-2" => new List<Network>
            {
                new Network
                {
                    Id = "demo-net-3",
                    OrganizationId = organizationId,
                    Name = "Store #42 - Downtown",
                    ProductTypes = new List<string> { "wireless", "switch", "camera", "sensor" },
                    Tags = new List<string> { "retail", "downtown" },
                    TimeZone = "America/New_York",
                    Url = "https://n123.meraki.com/demo-net-3/n/manage/nodes/list"
                },
                new Network
                {
                    Id = "demo-net-4",
                    OrganizationId = organizationId,
                    Name = "Store #89 - Shopping Mall",
                    ProductTypes = new List<string> { "wireless", "camera" },
                    Tags = new List<string> { "retail", "mall" },
                    TimeZone = "America/Chicago",
                    Url = "https://n123.meraki.com/demo-net-4/n/manage/nodes/list"
                }
            },
            _ => new List<Network>()
        };
    }

    /// <summary>
    /// Generates demo devices for a given organization ID
    /// </summary>
    public static List<Device> GenerateDevices(string organizationId)
    {
        return organizationId switch
        {
            "demo-org-1" => GenerateOrg1Devices(),
            "demo-org-2" => GenerateOrg2Devices(),
            _ => new List<Device>()
        };
    }

    private static List<Device> GenerateOrg1Devices()
    {
        var devices = new List<Device>();

        // Corporate Campus - Wireless APs
        devices.AddRange(new[]
        {
            new Device { Serial = "Q2AB-CORP-AP01", Name = "Building A - Floor 1 AP", NetworkId = "demo-net-1", Model = "MR46", ProductType = "wireless" },
            new Device { Serial = "Q2AB-CORP-AP02", Name = "Building A - Floor 2 AP", NetworkId = "demo-net-1", Model = "MR46", ProductType = "wireless" },
            new Device { Serial = "Q2AB-CORP-AP03", Name = "Building B - Floor 1 AP", NetworkId = "demo-net-1", Model = "MR56", ProductType = "wireless" },
            new Device { Serial = "Q2AB-CORP-AP04", Name = "Building B - Floor 2 AP", NetworkId = "demo-net-1", Model = "MR56", ProductType = "wireless" },
            new Device { Serial = "Q2AB-CORP-AP05", Name = "Lobby Main AP", NetworkId = "demo-net-1", Model = "MR86", ProductType = "wireless" },
            new Device { Serial = "Q2AB-CORP-AP06", Name = "Conference Center AP", NetworkId = "demo-net-1", Model = "MR86", ProductType = "wireless" }
        });

        // Corporate Campus - Switches
        devices.AddRange(new[]
        {
            new Device { Serial = "Q2BB-CORP-SW01", Name = "Building A - MDF Switch", NetworkId = "demo-net-1", Model = "MS225-24", ProductType = "switch" },
            new Device { Serial = "Q2BB-CORP-SW02", Name = "Building B - MDF Switch", NetworkId = "demo-net-1", Model = "MS225-48", ProductType = "switch" },
            new Device { Serial = "Q2BB-CORP-SW03", Name = "Server Room - Core Switch", NetworkId = "demo-net-1", Model = "MS350-24X", ProductType = "switch" }
        });

        // Corporate Campus - Security Appliances
        devices.AddRange(new[]
        {
            new Device { Serial = "Q2CB-CORP-MX01", Name = "Primary Security Appliance", NetworkId = "demo-net-1", Model = "MX75", ProductType = "appliance" },
            new Device { Serial = "Q2CB-CORP-MX02", Name = "Backup Security Appliance", NetworkId = "demo-net-1", Model = "MX75", ProductType = "appliance" }
        });

        // Corporate Campus - Cameras
        devices.AddRange(new[]
        {
            new Device { Serial = "Q2DB-CORP-MV01", Name = "Main Entrance Camera", NetworkId = "demo-net-1", Model = "MV22", ProductType = "camera" },
            new Device { Serial = "Q2DB-CORP-MV02", Name = "Parking Lot Camera 1", NetworkId = "demo-net-1", Model = "MV32", ProductType = "camera" },
            new Device { Serial = "Q2DB-CORP-MV03", Name = "Parking Lot Camera 2", NetworkId = "demo-net-1", Model = "MV32", ProductType = "camera" },
            new Device { Serial = "Q2DB-CORP-MV04", Name = "Lobby Camera", NetworkId = "demo-net-1", Model = "MV12W", ProductType = "camera" }
        });

        // Guest WiFi Network - Wireless APs
        devices.AddRange(new[]
        {
            new Device { Serial = "Q2AB-GUEST-AP01", Name = "Guest AP - Lobby", NetworkId = "demo-net-2", Model = "MR56", ProductType = "wireless" },
            new Device { Serial = "Q2AB-GUEST-AP02", Name = "Guest AP - Cafeteria", NetworkId = "demo-net-2", Model = "MR56", ProductType = "wireless" },
            new Device { Serial = "Q2AB-GUEST-AP03", Name = "Guest AP - Waiting Area", NetworkId = "demo-net-2", Model = "MR46", ProductType = "wireless" },
            new Device { Serial = "Q2AB-GUEST-AP04", Name = "Guest AP - Conference Room A", NetworkId = "demo-net-2", Model = "MR46", ProductType = "wireless" },
            new Device { Serial = "Q2AB-GUEST-AP05", Name = "Guest AP - Conference Room B", NetworkId = "demo-net-2", Model = "MR46", ProductType = "wireless" }
        });

        return devices;
    }

    private static List<Device> GenerateOrg2Devices()
    {
        var devices = new List<Device>();

        // Store #42 - Downtown - Wireless APs
        devices.AddRange(new[]
        {
            new Device { Serial = "Q2AB-ST42-AP01", Name = "Store 42 - Front Section AP", NetworkId = "demo-net-3", Model = "MR46", ProductType = "wireless" },
            new Device { Serial = "Q2AB-ST42-AP02", Name = "Store 42 - Back Section AP", NetworkId = "demo-net-3", Model = "MR46", ProductType = "wireless" },
            new Device { Serial = "Q2AB-ST42-AP03", Name = "Store 42 - Stockroom AP", NetworkId = "demo-net-3", Model = "MR36", ProductType = "wireless" },
            new Device { Serial = "Q2AB-ST42-AP04", Name = "Store 42 - Office AP", NetworkId = "demo-net-3", Model = "MR36", ProductType = "wireless" }
        });

        // Store #42 - Switches
        devices.AddRange(new[]
        {
            new Device { Serial = "Q2BB-ST42-SW01", Name = "Store 42 - Main Switch", NetworkId = "demo-net-3", Model = "MS120-8", ProductType = "switch" },
            new Device { Serial = "Q2BB-ST42-SW02", Name = "Store 42 - POS Switch", NetworkId = "demo-net-3", Model = "MS120-8", ProductType = "switch" }
        });

        // Store #42 - Cameras
        devices.AddRange(new[]
        {
            new Device { Serial = "Q2DB-ST42-MV01", Name = "Store 42 - Entrance Camera", NetworkId = "demo-net-3", Model = "MV12W", ProductType = "camera" },
            new Device { Serial = "Q2DB-ST42-MV02", Name = "Store 42 - Sales Floor Camera", NetworkId = "demo-net-3", Model = "MV22", ProductType = "camera" },
            new Device { Serial = "Q2DB-ST42-MV03", Name = "Store 42 - Cash Register Camera", NetworkId = "demo-net-3", Model = "MV12W", ProductType = "camera" }
        });

        // Store #42 - Sensors
        devices.AddRange(new[]
        {
            new Device { Serial = "Q2EB-ST42-MT01", Name = "Store 42 - Main Door Sensor", NetworkId = "demo-net-3", Model = "MT20", ProductType = "sensor" },
            new Device { Serial = "Q2EB-ST42-MT02", Name = "Store 42 - Back Door Sensor", NetworkId = "demo-net-3", Model = "MT20", ProductType = "sensor" },
            new Device { Serial = "Q2EB-ST42-MT03", Name = "Store 42 - Temperature Sensor", NetworkId = "demo-net-3", Model = "MT40", ProductType = "sensor" }
        });

        // Store #89 - Shopping Mall - Wireless APs
        devices.AddRange(new[]
        {
            new Device { Serial = "Q2AB-ST89-AP01", Name = "Store 89 - Front AP", NetworkId = "demo-net-4", Model = "MR86", ProductType = "wireless" },
            new Device { Serial = "Q2AB-ST89-AP02", Name = "Store 89 - Center AP", NetworkId = "demo-net-4", Model = "MR86", ProductType = "wireless" },
            new Device { Serial = "Q2AB-ST89-AP03", Name = "Store 89 - Back Office AP", NetworkId = "demo-net-4", Model = "MR36", ProductType = "wireless" }
        });

        // Store #89 - Cameras
        devices.AddRange(new[]
        {
            new Device { Serial = "Q2DB-ST89-MV01", Name = "Store 89 - Entrance Camera", NetworkId = "demo-net-4", Model = "MV32", ProductType = "camera" },
            new Device { Serial = "Q2DB-ST89-MV02", Name = "Store 89 - Floor Camera 1", NetworkId = "demo-net-4", Model = "MV32", ProductType = "camera" },
            new Device { Serial = "Q2DB-ST89-MV03", Name = "Store 89 - Floor Camera 2", NetworkId = "demo-net-4", Model = "MV22", ProductType = "camera" },
            new Device { Serial = "Q2DB-ST89-MV04", Name = "Store 89 - POS Camera", NetworkId = "demo-net-4", Model = "MV12W", ProductType = "camera" },
            new Device { Serial = "Q2DB-ST89-MV05", Name = "Store 89 - Exit Camera", NetworkId = "demo-net-4", Model = "MV22", ProductType = "camera" }
        });

        return devices;
    }
}

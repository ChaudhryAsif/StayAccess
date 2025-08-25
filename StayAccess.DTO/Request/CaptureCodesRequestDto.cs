using System.Collections.Generic;

namespace StayAccess.DTO.Request
{
    public class CaptureCodesRequestDto
    {
        public int id { get; set; }
        public string name { get; set; }
        public string loc { get; set; }
        public List<Value> values { get; set; }
        public List<Group> groups { get; set; }
        public List<object> neighbors { get; set; }
        public bool ready { get; set; }
        public bool available { get; set; }
        public HassDevices hassDevices { get; set; }
        public bool failed { get; set; }
        public bool inited { get; set; }
        public string hexId { get; set; }
        public string dbLink { get; set; }
        public int manufacturerId { get; set; }
        public int productId { get; set; }
        public int productType { get; set; }
        public DeviceConfig deviceConfig { get; set; }
        public string productLabel { get; set; }
        public string productDescription { get; set; }
        public string manufacturer { get; set; }
        public string firmwareVersion { get; set; }
        public int protocolVersion { get; set; }
        public int zwavePlusVersion { get; set; }
        public int zwavePlusNodeType { get; set; }
        public int zwavePlusRoleType { get; set; }
        public int nodeType { get; set; }
        public int endpointsCount { get; set; }
        public List<object> endpointIndizes { get; set; }
        public bool isSecure { get; set; }
        public string security { get; set; }
        public bool supportsSecurity { get; set; }
        public bool supportsBeaming { get; set; }
        public bool isControllerNode { get; set; }
        public bool isListening { get; set; }
        public string isFrequentListening { get; set; }
        public bool isRouting { get; set; }
        public bool keepAwake { get; set; }
        public int maxDataRate { get; set; }
        public DeviceClass deviceClass { get; set; }
        public string deviceId { get; set; }
        public string status { get; set; }
        public string interviewStage { get; set; }
        public Statistics statistics { get; set; }
        public long lastActive { get; set; }
        public int minBatteryLevel { get; set; }
        public Dictionary<string, int> batteryLevels { get; set; }
    }

    public class Value
    {
        public string id { get; set; }
        public int nodeId { get; set; }
        public int homeId { get; set; }
        public int commandClass { get; set; }
        public string commandClassName { get; set; }
        public int endpoint { get; set; }
        public string property { get; set; }
        public string propertyName { get; set; }
        public int propertyKey { get; set; }
        public string propertyKeyName { get; set; }
        public string type { get; set; }
        public bool readable { get; set; }
        public bool writeable { get; set; }
        public string label { get; set; }
        public bool stateless { get; set; }
        public int commandClassVersion { get; set; }
        public int min { get; set; }
        public int max { get; set; }
        public string unit { get; set; }
        public bool list { get; set; }
        public int value { get; set; }
        public long lastUpdate { get; set; }
    }

    public class Group
    {
        public string text { get; set; }
        public int endpoint { get; set; }
        public int value { get; set; }
        public int maxNodes { get; set; }
        public bool isLifeline { get; set; }
        public bool multiChannel { get; set; }
    }

    public class Device
    {
        public List<string> identifiers { get; set; }
        public string manufacturer { get; set; }
        public string model { get; set; }
        public string name { get; set; }
        public string sw_version { get; set; }
    }

    public class DiscoveryPayload
    {
        public string command_topic { get; set; }
        public int state_locked { get; set; }
        public int state_unlocked { get; set; }
        public int payload_lock { get; set; }
        public int payload_unlock { get; set; }
        public string value_template { get; set; }
        public string state_topic { get; set; }
        public string json_attributes_topic { get; set; }
        public Device device { get; set; }
        public string name { get; set; }
        public string unique_id { get; set; }
        public string device_class { get; set; }
        public string unit_of_measurement { get; set; }
        public bool payload_on { get; set; }
        public bool payload_off { get; set; }
        public string icon { get; set; }
    }

    public class LockLock
    {
        public string type { get; set; }
        public string object_id { get; set; }
        public DiscoveryPayload discovery_payload { get; set; }
        public string discoveryTopic { get; set; }
        public List<string> values { get; set; }
        public bool persistent { get; set; }
        public bool ignoreDiscovery { get; set; }
    }

    public class SensorBatteryLevel
    {
        public string type { get; set; }
        public string object_id { get; set; }
        public DiscoveryPayload discovery_payload { get; set; }
        public string discoveryTopic { get; set; }
        public List<string> values { get; set; }
        public bool persistent { get; set; }
        public bool ignoreDiscovery { get; set; }
    }

    public class BinarySensorBatteryIslow
    {
        public string type { get; set; }
        public string object_id { get; set; }
        public DiscoveryPayload discovery_payload { get; set; }
        public string discoveryTopic { get; set; }
        public List<string> values { get; set; }
        public bool persistent { get; set; }
        public bool ignoreDiscovery { get; set; }
    }

    public class BinarySensorLockState
    {
        public string type { get; set; }
        public string object_id { get; set; }
        public DiscoveryPayload discovery_payload { get; set; }
        public string discoveryTopic { get; set; }
        public List<string> values { get; set; }
        public bool persistent { get; set; }
        public bool ignoreDiscovery { get; set; }
    }

    public class BinarySensorKeypadState
    {
        public string type { get; set; }
        public string object_id { get; set; }
        public DiscoveryPayload discovery_payload { get; set; }
        public string discoveryTopic { get; set; }
        public List<string> values { get; set; }
        public bool persistent { get; set; }
        public bool ignoreDiscovery { get; set; }
    }

    public class BinarySensorPowerStatus
    {
        public string type { get; set; }
        public string object_id { get; set; }
        public DiscoveryPayload discovery_payload { get; set; }
        public string discoveryTopic { get; set; }
        public List<string> values { get; set; }
        public bool persistent { get; set; }
        public bool ignoreDiscovery { get; set; }
    }

    public class SensorNotificationPowerManagementBatteryMaintenanceStatus
    {
        public string type { get; set; }
        public string object_id { get; set; }
        public DiscoveryPayload discovery_payload { get; set; }
        public string discoveryTopic { get; set; }
        public List<string> values { get; set; }
        public bool persistent { get; set; }
        public bool ignoreDiscovery { get; set; }
    }

    public class BinarySensorDoorState
    {
        public string type { get; set; }
        public string object_id { get; set; }
        public DiscoveryPayload discovery_payload { get; set; }
        public string discoveryTopic { get; set; }
        public List<string> values { get; set; }
        public bool persistent { get; set; }
        public bool ignoreDiscovery { get; set; }
    }

    public class HassDevices
    {
        public LockLock lock_lock { get; set; }
        public SensorBatteryLevel sensor_battery_level { get; set; }
        public BinarySensorBatteryIslow binary_sensor_battery_islow { get; set; }
        public BinarySensorLockState binary_sensor_lock_state { get; set; }
        public BinarySensorKeypadState binary_sensor_keypad_state { get; set; }
        public BinarySensorPowerStatus binary_sensor_power_status { get; set; }
        public SensorNotificationPowerManagementBatteryMaintenanceStatus sensor_notification_power_management_battery_maintenance_status { get; set; }
        public BinarySensorDoorState binary_sensor_door_state { get; set; }
    }

    public class Device9
    {
        public int productType { get; set; }
        public int productId { get; set; }
    }

    public class FirmwareVersion
    {
        public string min { get; set; }
        public string max { get; set; }
    }

    public class Map
    {
    }

    public class ParamInformation
    {
        public Map _map { get; set; }
    }

    public class Metadata
    {
        public string inclusion { get; set; }
        public string exclusion { get; set; }
        public string reset { get; set; }
        public string manual { get; set; }
    }

    public class DeviceConfig
    {
        public string filename { get; set; }
        public bool isEmbedded { get; set; }
        public string manufacturer { get; set; }
        public int manufacturerId { get; set; }
        public string label { get; set; }
        public string description { get; set; }
        public List<Device> devices { get; set; }
        public FirmwareVersion firmwareVersion { get; set; }
        public ParamInformation paramInformation { get; set; }
        public Metadata metadata { get; set; }
    }

    public class DeviceClass
    {
        public int basic { get; set; }
        public int generic { get; set; }
        public int specific { get; set; }
    }

    public class Statistics
    {
        public int commandsTX { get; set; }
        public int commandsRX { get; set; }
        public int commandsDroppedRX { get; set; }
        public int commandsDroppedTX { get; set; }
        public int timeoutResponse { get; set; }
    }
}

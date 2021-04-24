using System.Runtime.Serialization;

namespace Trackers.Models
{
    [DataContract]
    public class TrackerInfo
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Url { get; set; }
        
        [DataMember]
        public int Sort { get; set; }
    }
}
using System.Runtime.Serialization;

public class WssMessage{
    public string type { get; set; }
    public WssMessageBody body { get; set; }
}

public class WssMessageBody{
     [DataMember]
    public string id { get; set; }
     [DataMember]
    public string type { get; set; }
     [DataMember]
    public MssMessageBodyBody body { get; set; }
}

public class MssMessageBodyBody{
     [DataMember]
    public string id { get; set; }
     [DataMember]
    public string userId { get; set; }
     [DataMember]
    public MssMessageBodyBodyUser user { get; set; }
     [DataMember]
    public string? text { get; set; }

}

public class MssMessageBodyBodyUser{
     [DataMember]
    public string id { get; set; }
     [DataMember]
    public string name { get; set; }
     [DataMember]
    public string username { get; set; }
}

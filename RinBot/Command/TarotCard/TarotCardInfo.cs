using Newtonsoft.Json;

public class TarotCardInfo
{
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("name_en")]
    public string NameEN { get; set; }
    [JsonIgnore]
    public string ImagePath { get; set; }
    [JsonIgnore]
    public bool IsReversed { get; set; }
    [JsonProperty("info")]
    public CardInfo Info { get; set; }
    
    public class CardInfo
    {
        [JsonProperty("element")]
        public string Element { get; set; }
        [JsonProperty("match")]
        public string Match { get; set; }
        [JsonProperty("celestial")]
        public string Celestial { get; set; }
        [JsonProperty("keyword")]
        public string Keyword { get; set; }
        [JsonProperty("content")]
        public string Content { get; set; }
        [JsonProperty("describe")]
        public string Describe { get; set; }
        [JsonProperty("reverse_describe")]
        public string ReverseDescribe { get; set; }
    }
}



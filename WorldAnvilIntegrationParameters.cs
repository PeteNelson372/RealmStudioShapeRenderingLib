using System.Xml.Serialization;

namespace RealmStudioShapeRenderingLib
{
    public class WorldAnvilIntegrationParameters
    {
        [XmlIgnore]
        public string ApiToken { get; set; } = string.Empty;

        [XmlIgnore]
        public string ApiKey {  get; set; } = string.Empty;

        [XmlIgnore]
        public string WAUsername { get; set; } = string.Empty;

        [XmlIgnore]
        public string WAUserId { get; set; } = string.Empty;

        [XmlAttribute]
        public Guid WorldAnvilMapId { get; set; } = Guid.Empty;

        [XmlAttribute]
        public string WorldAnvilMapTitle { get; set; } = string.Empty;

        [XmlAttribute]
        public Guid WorldAnvilWorldId { get; set; } = Guid.Empty;

        [XmlAttribute]
        public string WorldAnvilWorldTitle { get; set; } = string.Empty;

        [XmlAttribute]
        public Guid WorldAnvilUserId { get; set; } = Guid.Empty;

        [XmlAttribute]
        public int WorldAnvilImageId { get; set; } = 0;

        [XmlAttribute]
        public string WorldAnvilArticleId { get; set; } = string.Empty;
    }
}

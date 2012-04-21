using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Globalization;

namespace Blacker.MangaScraper.Models
{
    public sealed class RecentItem : IXmlSerializable
    {
        public string Item { get; set; }

        public DateTime LastUse { get; set; }

        public int TimesUsed { get; set; }

        #region IXmlSerializable implementation

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == "RecentItem")
            {
                Item = reader["item"];
                DateTime lastUse;
                if (DateTime.TryParse(reader["lastUse"], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out lastUse))
                    LastUse = lastUse;
                else
                    LastUse = DateTime.MinValue;

                int timesUsed;
                if (int.TryParse(reader["timesUsed"], out timesUsed))
                    TimesUsed = timesUsed;
                else
                    TimesUsed = 0;

                reader.Read();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("item", Item);
            writer.WriteAttributeString("lastUse", LastUse.ToString("r"));
            writer.WriteAttributeString("timesUsed", TimesUsed.ToString());
        }

        #endregion // IXmlSerializable implementation
    }
}

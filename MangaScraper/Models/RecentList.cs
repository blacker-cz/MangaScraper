using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using log4net;

namespace Blacker.MangaScraper.Models
{
    public sealed class RecentList : IEnumerable<RecentItem>, IXmlSerializable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(RecentList));

        private readonly IList<RecentItem> _recentItems = new List<RecentItem>();

        public const uint MaxItemsDefault = 10;
        private uint _maxItems;

        public RecentList()
            : this(MaxItemsDefault)
        { }

        public RecentList(uint maxItems)
        {
            _maxItems = maxItems;
        }

        public uint MaxItems
        {
            get { return _maxItems; }
            set
            {
                _maxItems = value;
                RemoveExceeding(_maxItems);
            }
        }

        public void Add(string item)
        {
            var recentItem = _recentItems.FirstOrDefault(ri => ri.Item.Equals(item));
            if (recentItem == null)
            {
                recentItem = new RecentItem() {
                                        Item = item,
                                        LastUse = DateTime.UtcNow,
                                        TimesUsed = 1
                                    };

                _recentItems.Add(recentItem);
            }
            else
            {
                recentItem.LastUse = DateTime.UtcNow;
                recentItem.TimesUsed++;
            }

            RemoveExceeding();
        }

        public void Remove(string item)
        {
            var recentItem = _recentItems.FirstOrDefault(ri => ri.Item.Equals(item));
            if (recentItem != null)
                _recentItems.Remove(recentItem);
        }

        public void Clear()
        {
            _recentItems.Clear();
        }

        private void RemoveExceeding()
        {
            RemoveExceeding(MaxItems);
        }

        private void RemoveExceeding(uint maxItems)
        {
            while (_recentItems.Count > maxItems)
            {
                // remove oldest items (this should probably use some more intelligent algorithm)
                var oldest = _recentItems.Aggregate((curmin, x) => (curmin == null || x.LastUse < curmin.LastUse ? x : curmin));

                _recentItems.Remove(oldest);
            }
        }

        #region IXmlSerializable implementation

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            try
            {
                if (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == "RecentList")
                {
                    uint maxItems;
                    if (uint.TryParse(reader["maxItems"], out maxItems))
                        MaxItems = maxItems;
                    else
                        MaxItems = MaxItemsDefault;

                    if (reader.ReadToDescendant("RecentItem"))
                    {
                        while (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == "RecentItem")
                        {
                            RecentItem item = new RecentItem();
                            item.ReadXml(reader);
                            _recentItems.Add(item);
                        }
                    }
                    reader.Read();
                }
            }
            catch (Exception ex)
            {
                _log.Error("Unable to deserialize RecentList.", ex);
                throw;
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            try
            {
                writer.WriteAttributeString("maxItems", MaxItems.ToString());

                foreach (var item in _recentItems)
                {
                    writer.WriteStartElement("RecentItem");
                    item.WriteXml(writer);
                    writer.WriteEndElement();
                }
            }
            catch (Exception ex)
            {
                _log.Error("Unable to serialize RecentList.", ex);
                throw;
            }
        }

        #endregion // IXmlSerializable implementation

        #region IEnumerable implementation

        public IEnumerator<RecentItem> GetEnumerator()
        {
            return _recentItems.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (_recentItems as System.Collections.IEnumerable).GetEnumerator();
        }

        #endregion // IEnumerable implementation
    }
}

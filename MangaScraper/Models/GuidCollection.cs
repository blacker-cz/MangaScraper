using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using log4net;
using System.Xml;
using System.Xml.Schema;

namespace Blacker.MangaScraper.Models
{
    public sealed class GuidCollection : IEnumerable<Guid>, IXmlSerializable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(GuidCollection));

        private readonly ISet<Guid> _guids = new HashSet<Guid>();

        public bool Add(Guid guid)
        {
            if (guid == Guid.Empty || guid == null)
                throw new ArgumentException("guid cannot be empty");

            return _guids.Add(guid);
        }

        public bool Remove(Guid guid)
        {
            if (guid == Guid.Empty || guid == null)
                throw new ArgumentException("guid cannot be empty");

            return _guids.Remove(guid);
        }

        public void Clear()
        {
            _guids.Clear();
        }

        #region IEnumerable implementation

        public IEnumerator<Guid> GetEnumerator()
        {
            return _guids.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (_guids as System.Collections.IEnumerable).GetEnumerator();
        }

        #endregion // IEnumerable implementation

        #region IXmlSerializable implementation

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            try
            {
                if (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == "GuidCollection")
                {
                    if (reader.ReadToDescendant("Guid"))
                    {
                        while (reader.MoveToContent() == XmlNodeType.Element && reader.LocalName == "Guid")
                        {
                            var content = reader.ReadElementContentAsString();
                            Guid guid;
                            if (Guid.TryParse(content, out guid))
                            {
                                _guids.Add(guid);
                            }
                            else
                            {
                                _log.Warn(String.Format("Unable to parse guid '{0}'", content));
                            }
                        }
                    }
                    reader.Read();
                }
            }
            catch (Exception ex)
            {
                _log.Error("Unable to deserialize GuidCollection.", ex);
                throw;
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            try
            {
                foreach (var guid in _guids)
                {
                    writer.WriteStartElement("Guid");
                    writer.WriteString(guid.ToString());
                    writer.WriteEndElement();
                }
            }
            catch (Exception ex)
            {
                _log.Error("Unable to serialize GuidCollection.", ex);
                throw;
            }
        }

        #endregion // IXmlSerializable implementation
    }
}

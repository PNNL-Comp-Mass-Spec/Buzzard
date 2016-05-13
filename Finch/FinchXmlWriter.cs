using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Finch.Data;
using System.IO;
using System.Reflection;

namespace Finch
{
    public class FinchXmlWriter : IFinchWriter
    {
        private const string VERSION = "1.0";
        
        public FinchXmlWriter()
        {
            
        }
        private XmlElement WriteTuple(XmlDocument document, FinchDataTuple signal)
        {
            XmlElement element = document.CreateElement("tuple");
            element.SetAttribute("keytype",     signal.XDataType.ToString());
            element.SetAttribute("valuetype",   signal.YDataType.ToString());
            element.SetAttribute("keyunit",     signal.XUnits.ToString());
            element.SetAttribute("valueunit",   signal.YUnits.ToString());

            if (signal.XValues.Count != signal.YValues.Count)
            {
                element.InnerText = "";
                return element;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < signal.XValues.Count; i++)
            {
                builder.Append(string.Format("{0},{1};", signal.XValues[i], signal.YValues[i]));
            }
            element.InnerText = builder.ToString();

            return element;
        }
        private void WriteScalar(XmlElement signalElement, FinchScalarSignal signal)
        {
            signalElement.SetAttribute("unit", signal.Units.ToString());
            signalElement.InnerText = signal.Value;
        }
        private XmlElement WriteSignal(XmlDocument document, FinchSignalBase signal)
        {
            XmlElement signalElement = document.CreateElement("signal");
            signalElement.SetAttribute("name",          signal.Name);
            signalElement.SetAttribute("type",          signal.Type.ToString());

            signalElement.SetAttribute("lastupdate", signal.LastUpdated.ToString());
            
            FinchScalarSignal scalar = signal as FinchScalarSignal;
            if (scalar != null)
            {
                WriteScalar(signalElement, scalar);
            }
            else
            {
                XmlElement element = WriteTuple(document, signal as FinchDataTuple);
                if (element != null)
                {
                    signalElement.AppendChild(element);
                }
            }
                                   
            return signalElement;
        }
        private XmlElement WriteComponent(XmlDocument document, FinchComponentData component)
        {
            XmlElement componentElement = document.CreateElement("component");
            componentElement.SetAttribute("name",       component.Name);
            componentElement.SetAttribute("status",     component.Status);
            componentElement.SetAttribute("type",       component.Type);
            componentElement.SetAttribute("lastupdate", component.LastUpdate.ToString());

            foreach(FinchSignalBase signal in component.Signals)
            {
                XmlElement element = WriteSignal(document, signal);
                if (element != null)
                {
                    componentElement.AppendChild(element);
                }
            }

            return componentElement;
        }
        /// <summary>
        /// Writes a single device's status.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="root"></param>
        /// <param name="device"></param>
        private XmlElement WriteAggregate(XmlDocument document, FinchAggregateData aggregate)
        {
            if (aggregate == null)
                return null;

            XmlElement aggregateElement = document.CreateElement("aggregate");
            aggregateElement.SetAttribute("name",       aggregate.Name);
            aggregateElement.SetAttribute("status",     aggregate.Status);
            aggregateElement.SetAttribute("type",       aggregate.Type);
            aggregateElement.SetAttribute("lastupdate", aggregate.LastUpdate.ToString());


            foreach (FinchComponentData componentData in aggregate.Components)
            {
                XmlElement componentElement = WriteComponent(document, componentData);
                if (componentElement != null)
                {
                    aggregateElement.AppendChild(componentElement);
                }
            }
                                   

            return aggregateElement;            
        }

        #region IFinchWriter Members
        public void WriteAggregates(List<FinchAggregateData> aggregates,  string path)
        {

            using (TextWriter textWriter = File.CreateText(path))
            {
                using (XmlTextWriter writer = new XmlTextWriter(textWriter))
                {
                    writer.Formatting       = Formatting.Indented;
                    XmlDocument document    = new XmlDocument();

                    XmlElement fml          = document.CreateElement("fml");
                    fml.SetAttribute("version", VERSION);
                    fml.SetAttribute("lastupdate", DateTime.Now.ToString());

                    XmlElement aggregatesElement      = document.CreateElement("aggregates");


                    foreach (FinchAggregateData aggregate in aggregates)
                    {                        
                        XmlElement singleAggregateElement =  WriteAggregate(document, aggregate);
                        if (singleAggregateElement != null)
                        {
                            aggregatesElement.AppendChild(singleAggregateElement);
                        }
                    }
                    fml.AppendChild(aggregatesElement);                   
                    document.AppendChild(fml);
                    document.Save(writer);
                }
            }
        }
        #endregion
    }
}

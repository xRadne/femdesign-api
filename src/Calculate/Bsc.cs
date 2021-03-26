﻿// https://strusoft.com/
using System;
using System.IO;
using System.Xml.Serialization;


namespace FemDesign.Calculate
{
    /// <summary>
    /// fdscript.xsd
    /// fdscript root
    /// </summary>
    [XmlRoot("fdscript")]
    public class Bsc
    {
        [XmlAttribute("noNamespaceSchemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public const string XmlAttrib = "fdscript.xsd";
        [XmlElement("fdscriptheader", Order = 1)]
        public FdScriptHeader FdScriptHeader { get; set; } // FDSCRIPTHEADER
        [XmlElement("cmddoctable", Order = 2)]
        public DocTable DocTable { get; set; } // CMDDOCKTABLE
        [XmlElement("cmdendsession", Order = 3)]
        public CmdEndSession CmdEndSession { get; set; } // CMDENDSESSION
        [XmlIgnore]
        internal string Cwd { get; set; } // current work directory, string
        [XmlIgnore]
        internal string BscPath { get; set; } // path to fdscript file, string

        /// <summary>
        /// Parameterless constructor for serialization.
        /// </summary>
        private Bsc()
        {

        }

        public Bsc(ResultType resultType, string bscPath)
        {
            if (Path.GetExtension(bscPath) != ".bsc")
            {
                throw new ArgumentException($"File path must be '.bsc' but got '{bscPath}'");
            }
            BscPath = bscPath;
            Cwd = Path.GetDirectoryName(bscPath);
            DocTable = new DocTable(resultType);
            FdScriptHeader = new FdScriptHeader("Generated script.", Path.Combine(Cwd, "logfile.log"));
            CmdEndSession = new CmdEndSession();
        }

        public Bsc(ResultType resultType, int caseIndex, string bscPath) : this(resultType, bscPath)
        {
            DocTable.CaseIndex = caseIndex;
        }

        /// <summary>
        /// Serialize bsc.
        /// </summary>
        public void SerializeBsc()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Bsc));
            using (TextWriter writer = new StreamWriter(this.BscPath))
            {
                serializer.Serialize(writer, this);
            }
        }
    }
}
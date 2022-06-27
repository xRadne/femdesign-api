﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using FemDesign;
using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using Microsoft.XmlDiffPatch;


namespace FemDesign.Models
{
    [TestClass()]
    public class ModelTests
    {
        public static Stream Serialize(object source)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            formatter.Serialize(stream, source);
            return stream;
        }

        public static T Deserialize<T>(Stream stream)
        {
            IFormatter formatter = new BinaryFormatter();
            stream.Position = 0;
            return (T)formatter.Deserialize(stream);
        }

        private bool AreEqual(object obj1, object obj2)
        {
            if (obj1 == null && obj2 == null) return true;
            if (obj1 == null || obj2 == null) return false;

            if (!obj1.GetType().Equals(obj2.GetType()))
            {
                return false;
            }

            Type type = obj1.GetType();
            if (type.IsPrimitive || typeof(string).Equals(type))
            {
                return obj1.Equals(obj2);
            }
            if (type.IsArray)
            {
                Array first = obj1 as Array;
                Array second = obj2 as Array;
                var en = first.GetEnumerator();
                int i = 0;
                while (en.MoveNext())
                {
                    if (!AreEqual(en.Current, second.GetValue(i)))
                        return false;
                    i++;
                }
            }
            else
            {
                foreach (PropertyInfo pi in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
                {
                    var val = pi.GetValue(obj1);
                    var tval = pi.GetValue(obj2);
                    if (!AreEqual(val, tval))
                        return false;
                }
                foreach (FieldInfo fi in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
                {
                    var val = fi.GetValue(obj1);
                    var tval = fi.GetValue(obj2);
                    if (!AreEqual(val, tval))
                        return false;
                }
            }
            return true;
        }

        public static T Clone<T>(T source)
        {
            return Deserialize<T>(Serialize(source));
        }

        [TestMethod("Construct Model")]
        public void ModelTest()
        {
            Model model = new Model(Country.S);
            
            Assert.IsNotNull(model, "Can construct model");
            Assert.IsTrue(model.Country == FemDesign.Country.S, "Should construct model with country code preserved");
        }

        /// <summary>
        /// Test if global test model can be deserialised from path and then serialised to string.
        /// To check which version the test file was generated in check source software attribute in file.
        /// </summary>
        [TestMethod("ReadWriteConsole")]
        public void ReadWriteConsole()
        {
            string input = "Model/global-test-model_IN.struxml";
            Model model = Model.DeserializeFromFilePath(input);
            Console.Write(model.SerializeToString());
        }

        /// <summary>
        /// Test if global test model can be deserialised from path and then serialised to file.
        /// The file can then be opened in FEM-Design to look for errors.
        /// To check which version the test file was generated in check source software attribute in file.
        /// </summary>
        [TestMethod("ReadWriteFile")]
        public void ReadWriteFile()
        {
            string input = "Model/global-test-model_IN.struxml";
            string output = "Model/global-test-model_OUT.struxml";
            Model model = Model.DeserializeFromFilePath(input);
            model.SerializeModel(output);
        }

        /// <summary>
        /// Test if the global model can be deep cloned.
        /// </summary>
        [TestMethod("DeepClone")]
        public void DeepClone()
        {
            string input = "Model/global-test-model_IN.struxml";
            Model model = Model.DeserializeFromFilePath(input);
            var clone = model.DeepClone();
            Console.Write(clone.SerializeToString());
        }

        private (int, int, long) GetFileInfo(string filePath)
        {
            int fileLineCount = File.ReadLines(filePath).Count();
            int numberOfCharacters = File.ReadAllLines(filePath).Sum(s => s.Length);
            FileInfo fileInfo = new FileInfo(filePath);
            long size = fileInfo.Length;

            return (fileLineCount, numberOfCharacters, size);
        }

        public bool GenerateDiffGram(string originalFile, string finalFile,
                                            XmlWriter diffGramWriter)
        {
            XmlDiff xmldiff = new XmlDiff(XmlDiffOptions.IgnoreChildOrder |
                                             XmlDiffOptions.IgnoreNamespaces |
                                             XmlDiffOptions.IgnorePrefixes);
            bool bIdentical = xmldiff.Compare(originalFile, finalFile, false, diffGramWriter);
            diffGramWriter.Close();
            return bIdentical;
        }

        [TestMethod("CompareInOut")]
        public void CompareInOut()
        {
            // The input file "global-test-model_MASTER.struxml" has been previously checked for
            // equivalence with the .struxml Deserialised from FD taking in consideration that
            // the FD and the API deserialise in different way.

            // NOTE
            // if We implement of add new attribute for the already created object (i.e. some bar attribute)
            // the test will fail as the file wil not be identical anymore

            string inputFile = "Model/global-test-model_MASTER.struxml";
            Model model = Model.DeserializeFromFilePath(inputFile);

            string outputFile = "Model/global-test-model_MASTER_OUT.struxml";
            model.SerializeModel(outputFile);

            (int fileLineCountMaster, int numberOfCharactersMaster, long sizeMaster) = GetFileInfo(inputFile);
            (int fileLineCountOut, int numberOfCharactersOut, long sizeOut) = GetFileInfo(outputFile);

            Console.WriteLine($"Master file has {fileLineCountMaster} number of lines");
            Console.WriteLine($"Output file has {fileLineCountOut} number of lines");

            Console.WriteLine($"Master file has {numberOfCharactersMaster} characters");
            Console.WriteLine($"Output file has {numberOfCharactersOut} characters");

            Console.WriteLine($"Master file is {sizeMaster} bytes");
            Console.WriteLine($"Output file is {sizeOut} bytes");

            int diffLine = fileLineCountOut - fileLineCountMaster;
            int diffCharacter = numberOfCharactersOut - numberOfCharactersMaster;
            long diffSize = sizeOut - sizeMaster;

            var diffGramWriter = XmlWriter.Create("Model/diffGram.xml");
            bool identical = GenerateDiffGram(inputFile, outputFile, diffGramWriter);

            Assert.IsTrue(diffLine == 0);
            Assert.IsTrue(diffCharacter == 0);
            Assert.IsTrue(diffSize == 0);
            Assert.IsTrue(identical);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;

namespace BabyNameChooseUtility
{
    internal class Program
    {
        private static readonly IDictionary<string, long> MostFrequentWords = new Dictionary<string, long>();

        private static readonly IDictionary<string, long> MostFrequentReduplicatedWords =
            new Dictionary<string, long>();

        private static readonly char[] Separators =
        {
            ' ', ',', ';', '.', '!', '"', ':', '?',
            '。', '！', '：', '，', '“', '”', '？',
            '\r', '\n', '\t'
        };

        private static void Main(string[] args)
        {
            Console.WriteLine("Please input your dictionary.");

            string fileName = Console.ReadLine();
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = @"chuci.epub";
            }

            if (Directory.Exists(fileName))
            {
                string[] files = Directory.GetFiles(fileName);
                foreach (string file in files)
                {
                    Console.WriteLine("Start to process : " + file);
                    ProcessEpub(file);
                }
            }
            else if (File.Exists(fileName))
            {
                Console.WriteLine("Start to process : " + fileName);
                ProcessEpub(fileName);
            }
            else
            {
                Console.WriteLine("File path is not exit");
            }

            Console.WriteLine("Process finishied...");
            Console.ReadKey();
        }

        private static void ProcessEpub(string fileName)
        {
            var stream = new MemoryStream();

            using (FileStream file = File.OpenRead(fileName))
            {
                file.Seek(0, SeekOrigin.Begin);
                file.CopyTo(stream);
                stream.Seek(0, SeekOrigin.Begin);
            }

            using (var s = new ZipInputStream(stream))
            {
                ReadEpub(s);
            }

            using (FileStream s = File.Create(@"Most frequent words.txt"))
            {
                using (var sw = new StreamWriter(s))
                {
                    foreach (var w in MostFrequentWords.OrderByDescending(e => e.Value))
                    {
                        sw.WriteLine("{0} : {1}", w.Key, w.Value);
                    }
                    sw.Close();
                }
            }

            using (FileStream s = File.Create(@"Most frequent duplicated words.txt"))
            {
                using (var sw = new StreamWriter(s))
                {
                    foreach (var w in MostFrequentReduplicatedWords.OrderByDescending(e => e.Value))
                    {
                        sw.WriteLine("{0} : {1}", w.Key, w.Value);
                    }
                }
            }
        }

        private static void ReadEpub(ZipInputStream zisStream)
        {
            if (zisStream == null)
            {
                return;
            }

            ZipEntry theEntry;

            while ((theEntry = zisStream.GetNextEntry()) != null)
            {
                Debug.WriteLine(theEntry.Name);

                string directoryName = Path.GetDirectoryName(theEntry.Name);
                string fileName = Path.GetFileName(theEntry.Name);
                string fileExtension = Path.GetExtension(theEntry.Name);

                if (!fileExtension.Equals(".xhtml") && !fileExtension.Equals(".html"))
                {
                    continue;
                }

#if true
                using (var memoryStream = new MemoryStream())
                {
                    zisStream.CopyTo(memoryStream);

                    memoryStream.Seek(0, SeekOrigin.Begin);

                    using (var sr = new StreamReader(memoryStream))
                    {
                        string tmpContent = sr.ReadToEnd();
                        var htt = new HtmlToText();
                        string text = htt.StripHTML(tmpContent);

                        string[] lines = text.Split(Separators, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string line in lines)
                        {
                            char[] words = line.ToCharArray();

                            char preWord = '\x0000';
                            foreach (char w in words)
                            {
                                if (!IsChineseWord(w))
                                {
                                    continue;
                                }

                                if (MostFrequentWords.ContainsKey(w.ToString()))
                                {
                                    MostFrequentWords[w.ToString()] = MostFrequentWords[w.ToString()] + 1;
                                }
                                else
                                {
                                    MostFrequentWords[w.ToString()] = 1;
                                }

                                if (preWord == w)
                                {
                                    string duplicatedWord = string.Format("{0}{0}", w);
                                    if (MostFrequentReduplicatedWords.ContainsKey(duplicatedWord))
                                    {
                                        MostFrequentReduplicatedWords[duplicatedWord] =
                                            MostFrequentReduplicatedWords[duplicatedWord] + 1;
                                    }
                                    else
                                    {
                                        MostFrequentReduplicatedWords[duplicatedWord] = 1;
                                    }
                                }

                                preWord = w;
                            }
                        }
                    }
                }
#else

                #region CreateFiles
                // create directory
                if (!string.IsNullOrEmpty(directoryName) && directoryName.Length > 0)
                {
                    Directory.CreateDirectory(directoryName);
                }

                //create file
                if (!string.IsNullOrEmpty(fileName))
                {
                    using (FileStream streamWriter = File.Create(theEntry.Name))
                    {
                        int size = 2048;
                        var data = new byte[2048];
                        while (true)
                        {
                            size = zisStream.Read(data, 0, data.Length);
                            if (size > 0)
                            {
                                streamWriter.Write(data, 0, size);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                } 
                #endregion
#endif
            }
        }

        private static bool IsChineseWord(char chr)
        {
            if (chr >= 0x4E00 && chr <= 0x9FFF)
                return true;

            return false;
        }
    }
}
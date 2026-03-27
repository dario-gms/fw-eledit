using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FWEledit
{
    public sealed class ConfigSequelScannerService
    {
        public void RunScan(ConfigData data, string configFolder, IWin32Window owner)
        {
            if (data == null)
            {
                return;
            }

            string configPath = Path.Combine(configFolder ?? string.Empty, "sequel_scanner.txt");
            ScanInfo[] siList = LoadSequelScannerConfiguration(data, configPath);
            if (siList == null)
            {
                return;
            }

            OpenFileDialog eLoad = new OpenFileDialog();
            eLoad.Filter = "EL (*.data)|*.data|All Files (*.*)|*.*";
            DialogResult result = owner == null ? eLoad.ShowDialog() : eLoad.ShowDialog(owner);
            if (result != DialogResult.OK || !File.Exists(eLoad.FileName))
            {
                return;
            }

            using (StreamReader sr = new StreamReader(eLoad.FileName))
            using (BinaryReader br = new BinaryReader(sr.BaseStream))
            {
                long streamPos;
                short version = br.ReadInt16();
                br.ReadInt16();

                string log = string.Empty;
                log += "Configuration File: " + data.LoadedFileName + "\r\n";
                log += "Element File Version: " + version.ToString() + "\r\n\r\n";

                for (int i = 0; i < data.ListNames.Length; i++)
                {
                    SkipOffset(data, version, i, br);
                    if (i == data.ConversationListIndex)
                    {
                        SkipConversationList(data, version, i, br);
                        log += i.ToString("D3") + ": SKIPPED (Conversation List)\r\n";
                        i++;
                    }

                    log += i.ToString("D3") + ":";
                    siList[i].ElementCount = br.ReadInt32();
                    streamPos = br.BaseStream.Position;

                    if (siList[i].ElementCount > 0)
                    {
                        if (siList[i].FirstElementID > -1)
                        {
                            if (siList[i].SecondElementID > -1)
                            {
                                br.ReadBytes(siList[i].EntrySizePrior);
                                siList[i].EntrySizeEstimated = siList[i].EntrySizePrior;
                                while (br.ReadInt32() != siList[i].SecondElementID)
                                {
                                    siList[i].EntrySizeEstimated++;
                                    br.BaseStream.Position -= 3;
                                }
                                br.BaseStream.Position = streamPos + (siList[i].ElementCount * siList[i].EntrySizeEstimated);

                                if (siList[i].EntrySizePrior != siList[i].EntrySizeEstimated)
                                {
                                    log += " CHANGED: Entry Size Increased (" + siList[i].EntrySizePrior.ToString() + " -> " + siList[i].EntrySizeEstimated.ToString() + ", +" + (siList[i].EntrySizeEstimated - siList[i].EntrySizePrior).ToString() + ")";
                                }
                                else
                                {
                                    log += " -";
                                }
                            }
                            else
                            {
                                if (i < siList.Length - 1)
                                {
                                    if (siList[i + 1].FirstElementID > -1)
                                    {
                                        if (data.ListOffsets[i + 1] == "AUTO" || ((i + 1 == 20 || i + 1 == 100) && version > 8 && data.LoadedFileName.EndsWith("_v7.cfg")))
                                        {
                                            if (MessageBox.Show("List Index: " + i.ToString() + "\r\n\r\nDeep scan through Offset:AUTO in next list not possible.\r\nTry to use EntrySize from configuration file?\r\n*Application may crash when EntrySize has been increased!", data.ListNames[i], MessageBoxButtons.YesNo) == DialogResult.Yes)
                                            {
                                                siList[i].EntrySizeEstimated = siList[i].EntrySizePrior;
                                                br.BaseStream.Position = streamPos + (siList[i].ElementCount * siList[i].EntrySizeEstimated);
                                                log += " INHERITED: Using Entry Size from Configuration File (" + siList[i].EntrySizeEstimated + ")";
                                            }
                                            else
                                            {
                                                log += " ABORT (Can't deep scan through OFFSET:AUTO)\r\n";
                                                goto ABORT;
                                            }
                                        }
                                        else
                                        {
                                            br.BaseStream.Position += (siList[i].ElementCount * siList[i].EntrySizePrior);
                                            while (br.ReadInt32() != siList[i + 1].FirstElementID)
                                            {
                                                br.BaseStream.Position -= 3;
                                            }
                                            br.BaseStream.Position -= (Convert.ToInt32(data.ListOffsets[i + 1]) + 4 + 4);
                                            siList[i].EntrySizeEstimated = (int)((br.BaseStream.Position - streamPos) / siList[i].ElementCount);

                                            if (siList[i].EntrySizePrior != siList[i].EntrySizeEstimated)
                                            {
                                                log += " CHANGED: Entry Size Increased (" + siList[i].EntrySizePrior.ToString() + " -> " + siList[i].EntrySizeEstimated.ToString() + ", +" + (siList[i].EntrySizeEstimated - siList[i].EntrySizePrior).ToString() + ")";
                                            }
                                            else
                                            {
                                                log += " -";
                                            }
                                        }
                                    }
                                    else
                                    {
                                        log += " ABORT (Can't deep scan through multiple unknown lists)\r\n";
                                        goto ABORT;
                                    }
                                }
                                else
                                {
                                    br.BaseStream.Position += (siList[i].ElementCount * siList[i].EntrySizePrior);
                                    if (br.BaseStream.Length != br.BaseStream.Position)
                                    {
                                        log += " ABORT (Can't deep scan through next [non-existing] list)\r\n";
                                        goto ABORT;
                                    }
                                    else
                                    {
                                        siList[i].EntrySizeEstimated = siList[i].EntrySizePrior;
                                        log += " INHERITED: Using Entry Size from Configuration File (" + siList[i].EntrySizeEstimated + ")";
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (false)
                            {
                                log += " ABORT (Error in sequel_scanner.txt)\r\n";
                                goto ABORT;
                            }
                            else
                            {
                                if (i < siList.Length - 1)
                                {
                                    if (siList[i + 1].FirstElementID > -1)
                                    {
                                        log += " ABORT (mot implemented yet)\r\n";
                                        goto ABORT;
                                    }
                                    else
                                    {
                                        log += " ABORT (Can't deep scan through multiple unknown lists)\r\n";
                                        goto ABORT;
                                    }
                                }
                                else
                                {
                                    br.BaseStream.Position += (siList[i].ElementCount * siList[i].EntrySizePrior);
                                    if (br.BaseStream.Length != br.BaseStream.Position)
                                    {
                                        log += " ABORT (Can't deep scan through next [non-existing] list)\r\n";
                                        goto ABORT;
                                    }
                                    else
                                    {
                                        siList[i].EntrySizeEstimated = siList[i].EntrySizePrior;
                                        log += " INHERITED: Using Entry Size from Configuration File (" + siList[i].EntrySizeEstimated + ")";
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        log += " SKIPPED (Empty List)";
                    }

                    log += "\r\n";
                }

            ABORT:

                log += "\r\nBYTES LEFT: " + (br.BaseStream.Length - br.BaseStream.Position).ToString();

                new DebugWindow("Scan Results", log);
            }
        }

        private int GetTypeSize(string type)
        {
            if (type == "int16")
            {
                return 2;
            }
            if (type == "int32")
            {
                return 4;
            }
            if (type == "int64")
            {
                return 8;
            }
            if (type == "float")
            {
                return 4;
            }
            if (type == "double")
            {
                return 8;
            }
            if (type.Contains("byte:"))
            {
                return Convert.ToInt32(type.Split(new char[] { ':' })[1]);
            }
            if (type.Contains("wstring:"))
            {
                return Convert.ToInt32(type.Split(new char[] { ':' })[1]);
            }
            if (type.Contains("string:"))
            {
                return Convert.ToInt32(type.Split(new char[] { ':' })[1]);
            }

            return 0;
        }

        private int GetElementTypeSize(ConfigData data, int listIndex)
        {
            if (data == null || data.FieldTypes == null)
            {
                return 0;
            }

            int size = 0;
            for (int i = 0; i < data.FieldTypes[listIndex].Length; i++)
            {
                size += GetTypeSize(data.FieldTypes[listIndex][i]);
            }
            return size;
        }

        private ScanInfo[] LoadSequelScannerConfiguration(ConfigData data, string file)
        {
            if (data == null || data.ListNames == null || !File.Exists(file))
            {
                return null;
            }

            StreamReader sr = new StreamReader(file);

            ScanInfo[] siList = new ScanInfo[data.ListNames.Length];
            ScanInfo si;
            string line;
            int i = 0;

            while (i < siList.Length)
            {
                try
                {
                    line = sr.ReadLine();
                    if (!line.StartsWith("#"))
                    {
                        si = new ScanInfo();

                        si.EntrySizePrior = -1;
                        si.EntrySizeEstimated = -1;

                        if (i != data.ConversationListIndex)
                        {
                            si.EntrySizePrior = GetElementTypeSize(data, i);
                        }
                        if (line == "NULL")
                        {
                            si.FirstElementID = -1;
                        }
                        else
                        {
                            si.FirstElementID = Convert.ToInt32(line);
                        }

                        line = sr.ReadLine();

                        if (line == "NULL")
                        {
                            si.SecondElementID = -1;
                        }
                        else
                        {
                            si.SecondElementID = Convert.ToInt32(line);
                        }

                        siList[i] = si;
                        i++;
                    }
                }
                catch
                {
                }
            }

            sr.Close();

            for (int ifo = i; ifo < siList.Length; ifo++)
            {
                si = new ScanInfo();
                si.EntrySizePrior = GetElementTypeSize(data, ifo);
                si.FirstElementID = -1;
                si.SecondElementID = -1;
                siList[ifo] = si;
            }

            return siList;
        }

        private void SkipOffset(ConfigData data, int version, int listIndex, BinaryReader br)
        {
            if (data == null)
            {
                return;
            }

            string offset = data.ListOffsets[listIndex];

            if (data.LoadedFileName.EndsWith("_v7.cfg") && version > 8)
            {
                if (listIndex == 0)
                {
                    offset = "4";
                }
                if (listIndex == 20 || listIndex == 100)
                {
                    offset = "AUTO";
                }
            }

            if (offset != string.Empty && offset != "0")
            {
                if (offset == "AUTO")
                {
                    if (listIndex == 0)
                    {
                        br.ReadBytes(4);
                        int count = br.ReadInt32();
                        br.ReadBytes(count);
                    }
                    if (listIndex == 20)
                    {
                        br.ReadBytes(4);
                        int count = br.ReadInt32();
                        br.ReadBytes(count);
                        br.ReadBytes(4);
                    }
                    int npcWarTowerbuildServiceIndex = 100;
                    if (version >= 191)
                    {
                        npcWarTowerbuildServiceIndex = 99;
                    }
                    if (listIndex == npcWarTowerbuildServiceIndex)
                    {
                        br.ReadBytes(4);
                        int count = br.ReadInt32();
                        br.ReadBytes(count);
                    }
                }
                else
                {
                    br.ReadBytes(Convert.ToInt32(offset));
                }
            }
        }

        private void SkipConversationList(ConfigData data, short version, int listIndex, BinaryReader br)
        {
            if (data == null)
            {
                return;
            }

            if (version >= 191)
            {
                bool run = true;
                while (run)
                {
                    run = false;
                    try
                    {
                        br.ReadByte();
                        run = true;
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                if (data.FieldTypes[listIndex][0].Contains("AUTO"))
                {
                    byte[] pattern = (Encoding.GetEncoding("GBK")).GetBytes("facedata\\");
                    bool run = true;
                    while (run)
                    {
                        run = false;
                        for (int i = 0; i < pattern.Length; i++)
                        {
                            if (br.ReadByte() != pattern[i])
                            {
                                run = true;
                                break;
                            }
                        }
                    }
                    br.BaseStream.Position -= (4 + 4 + 64 + pattern.Length);
                }
                else
                {
                    br.BaseStream.Position += Convert.ToInt32(data.FieldTypes[listIndex][0].Split(new char[] { ':' })[0]) + Convert.ToInt32(data.ListOffsets[listIndex]);
                }
            }
        }
    }
}

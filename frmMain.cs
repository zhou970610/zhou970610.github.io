using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Numerics;

namespace WordsForDaysDictionary
{
     public partial class frmMain : Form
     {
          const string DataRepositoryPath = "D:\\Games\\WordsForDays";
          const string DataRepositoryFileName = "Dictionary.txt";
          const string DataRepositoryFullPath = DataRepositoryPath + "\\" + DataRepositoryFileName;
          const char DataSeparator = '|';
          private List<DictionaryData> FileDatas = new List<DictionaryData>();
          public frmMain()
          {
               InitializeComponent();
               txtBlurFind.Enter += TextBox_ClickToSelectAll;
               txtTargetWord.Enter += TextBox_ClickToSelectAll;
               if (File.Exists(DataRepositoryFullPath))
               {
                    LoadDataFromFile();
               }
          }

          private bool LoadDataFromFile()
          {
               try
               {
                    FileDatas.Clear();
                    using (StreamReader sr = new StreamReader(DataRepositoryFullPath))
                    {
                         string line;
                         while ((line = sr.ReadLine()) != null)
                         {
                              if (!string.IsNullOrWhiteSpace(line))
                              {
                                   var data = ParseLineToDictionaryData(line);
                                   if (data != null)
                                   {
                                        FileDatas.Add(data);
                                   }
                              }
                         }
                    }
                    FileDatas.Sort((x, y) => string.Compare(x.CraftItem3, y.CraftItem3, StringComparison.OrdinalIgnoreCase));
                    BlurFind_StartIndex = 0;
                    BlurFind_EndIndex = FileDatas.Count - 1;
                    return true;
               }
               catch (Exception ex)
               {
                    MessageBox.Show("Error loading data: " + ex.Message);
                    return false;
               }
          }

          private void SaveDataToFile()
          {
               try
               {
                    using (StreamWriter sw = new StreamWriter(DataRepositoryFullPath, false))
                    {
                         foreach (var data in FileDatas)
                         {
                              string line = ParseDictionaryDataToLine(data);
                              if (!string.IsNullOrWhiteSpace(line))
                              {
                                   sw.WriteLine(line);
                              }
                         }
                    }
               }
               catch (Exception ex)
               {
                    MessageBox.Show("Error saving data: " + ex.Message);
               }
          }

          private DictionaryData ParseLineToDictionaryData(string line)
          {
               string[] parts = line.Split(DataSeparator);
               DictionaryData newData = new DictionaryData();
               if (parts.Length == typeof(DictionaryData).GetProperties().Length - 3)
               {
                    newData.CraftItem1 = parts[0].Trim().Substring(parts[0].Trim().IndexOf(' ') + 1);
                    newData.CraftItem1_emoji = parts[0].Trim().Substring(0, parts[0].Trim().IndexOf(' '));
                    newData.CraftItem2 = parts[1].Trim().Substring(parts[1].Trim().IndexOf(' ') + 1);
                    newData.CraftItem2_emoji = parts[1].Trim().Substring(0, parts[1].Trim().IndexOf(' '));
                    newData.CraftItem3 = parts[2].Trim().Substring(parts[2].Trim().IndexOf(' ') + 1);
                    newData.CraftItem3_emoji = parts[2].Trim().Substring(0, parts[2].Trim().IndexOf(' '));
                    newData.Hist1 = parts[3].Trim();
                    newData.Hist2 = parts[4].Trim();
                    newData.CraftItemCount = long.Parse(parts[5]);
                    return newData;
               }
               else
               {
                    MessageBox.Show("Invalid data format: " + line);
                    return null;
               }
          }
          private string ParseDictionaryDataToLine(DictionaryData Data)
          {
               if (Data == null)
               {
                    return string.Empty;
               }
               return $"{Data.CraftItem1_emoji} {Data.CraftItem1}{DataSeparator}{Data.CraftItem2_emoji} {Data.CraftItem2}{DataSeparator}{Data.CraftItem3_emoji} {Data.CraftItem3}{DataSeparator}{Data.Hist1}{DataSeparator}{Data.Hist2}{DataSeparator}{Data.CraftItemCount}";
          }

          private void btnAddDataByFile_Click(object sender, EventArgs e)
          {
               OpenFileDialog ofd = new OpenFileDialog();
               string FileName = "";
               ofd.ShowDialog();
               FileName = ofd.FileName;
               FileInfo fi = new FileInfo(FileName);
               if (fi.Exists)
               {
                    StreamReader sr = new StreamReader(fi.OpenRead());
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                         DecodeOriginTxtToData(line);
                    }
               }
               else
               {
                    MessageBox.Show("File does not exist.");
               }
          }

          private void DecodeOriginTxtToData(string line)
          {
               List<DictionaryData> curDatas = new List<DictionaryData>();
               //先找到crafting-entry，後面就會跟著crafting-item, hist, crafting-item, hist, crafting-item
               while (true)
               {
                    int entry_index = line.IndexOf("crafting-entry", StringComparison.OrdinalIgnoreCase);
                    if (entry_index == -1) break;
                    int item1_index = line.IndexOf("crafting-item", entry_index, StringComparison.OrdinalIgnoreCase);
                    if (item1_index == -1) break;
                    int hist1_index = line.IndexOf("hist", item1_index, StringComparison.OrdinalIgnoreCase);
                    if (hist1_index == -1) break;
                    int item2_index = line.IndexOf("crafting-item", hist1_index, StringComparison.OrdinalIgnoreCase);
                    if (item2_index == -1) break;
                    int hist2_index = line.IndexOf("hist", item2_index, StringComparison.OrdinalIgnoreCase);
                    if (hist2_index == -1) break;
                    int item3_index = line.IndexOf("crafting-item", hist2_index, StringComparison.OrdinalIgnoreCase);
                    if (item3_index == -1) break;

                    //找到所有的index後，開始解析
                    DictionaryData newData = new DictionaryData();
                    int bracket_start_index = line.IndexOf(">", item1_index, StringComparison.OrdinalIgnoreCase);
                    int bracket_end_index = line.IndexOf("<", item1_index, StringComparison.OrdinalIgnoreCase);
                    newData.CraftItem1 = line.Substring(bracket_start_index + 1, bracket_end_index - bracket_start_index - 1).Trim();
                    newData.CraftItem1_emoji = newData.CraftItem1.Substring(0, newData.CraftItem1.IndexOf(' '));
                    newData.CraftItem1 = newData.CraftItem1.Substring(newData.CraftItem1.IndexOf(' ') + 1);
                    bracket_start_index = line.IndexOf(">", item2_index, StringComparison.OrdinalIgnoreCase);
                    bracket_end_index = line.IndexOf("<", item2_index, StringComparison.OrdinalIgnoreCase);
                    newData.CraftItem2 = line.Substring(bracket_start_index + 1, bracket_end_index - bracket_start_index - 1).Trim();
                    newData.CraftItem2_emoji = newData.CraftItem2.Substring(0, newData.CraftItem2.IndexOf(' '));
                    newData.CraftItem2 = newData.CraftItem2.Substring(newData.CraftItem2.IndexOf(' ') + 1);
                    bracket_start_index = line.IndexOf(">", item3_index, StringComparison.OrdinalIgnoreCase);
                    bracket_end_index = line.IndexOf("<", item3_index, StringComparison.OrdinalIgnoreCase);
                    newData.CraftItem3 = line.Substring(bracket_start_index + 1, bracket_end_index - bracket_start_index - 1).Trim();
                    newData.CraftItem3_emoji = newData.CraftItem3.Substring(0, newData.CraftItem3.IndexOf(' '));
                    newData.CraftItem3 = newData.CraftItem3.Substring(newData.CraftItem3.IndexOf(' ') + 1);
                    bracket_start_index = line.IndexOf(">", hist1_index, StringComparison.OrdinalIgnoreCase);
                    bracket_end_index = line.IndexOf("<", hist1_index, StringComparison.OrdinalIgnoreCase);
                    newData.Hist1 = line.Substring(bracket_start_index + 1, bracket_end_index - bracket_start_index - 1).Trim();
                    bracket_start_index = line.IndexOf(">", hist2_index, StringComparison.OrdinalIgnoreCase);
                    bracket_end_index = line.IndexOf("<", hist2_index, StringComparison.OrdinalIgnoreCase);
                    newData.Hist2 = line.Substring(bracket_start_index + 1, bracket_end_index - bracket_start_index - 1).Trim();
                    newData.CraftItemCount = 0;
                    if (newData.CraftItem1 != "" && newData.CraftItem2 != "" && newData.CraftItem3 != "" && newData.Hist1 != "" && newData.Hist2 != "")
                    {
                         curDatas.Add(newData);
                    }
                    else
                    {
                         MessageBox.Show("Invalid data found in file: " + line);
                    }

                    //把line從已經處理過的部分切掉，繼續下一個循環
                    line = line.Substring(item3_index + 1);
                    if (line.Length == 0) break; //如果line已經沒有內容了，就跳出循環
               }

               //現在curDatas裡面已經有了所有的DictionaryData，開始計算CraftItemCount
               for (int i = 0; i < curDatas.Count; i++)
               {
                    FindCount(curDatas, i);
               }

               //比對這次資料跟歷史資料中誰的Count比較少並取代那一行資料
               foreach (var newData in curDatas)
               {
                    int index = FileDatas.FindIndex(data => data.CraftItem3 == newData.CraftItem3);
                    if (index != -1)
                    {
                         //如果已經存在，就更新
                         if (FileDatas[index].CraftItemCount == 0 || FileDatas[index].CraftItemCount > newData.CraftItemCount)
                         {
                              FileDatas[index] = newData;
                         }
                    }
                    else
                    {
                         //如果不存在，就新增
                         FileDatas.Add(newData);
                    }
               }
               SaveDataToFile();
          }

          private BigInteger FindCount(List<DictionaryData> DataList, int ListIndex)
          {
               DictionaryData data = DataList[ListIndex];
               if (data.CraftItemCount > 0)
               {
                    return data.CraftItemCount; //如果已經計算過了，就直接返回
               }
               BigInteger count = 1;
               BigInteger addCount1 = 0;
               BigInteger addCount2 = 0;
               if (data.CraftItem3 == "")
               {
                    return 0; //如果CraftItem3是空的，就不需要計算
               }
               if (data.CraftItem1 == "🌍 earth" || data.CraftItem1 == "🔥 fire" || data.CraftItem1 == "💧 water" || data.CraftItem1 == "💨 wind")
               {
                    //初始單字，不計數
                    addCount1 = 0;
               }
               else
               {
                    int itemIndex = DataList.FindIndex(finddata => finddata.CraftItem3 == data.CraftItem1);
                    if (itemIndex != -1)
                    {
                         addCount1 = FindCount(DataList, itemIndex);
                    }
               }
               if (data.CraftItem2 == "🌍 earth" || data.CraftItem2 == "🔥 fire" || data.CraftItem2 == "💧 water" || data.CraftItem2 == "💨 wind")
               {
                    //初始單字，不計數
                    addCount2 = 0;
               }
               else
               {
                    int itemIndex = DataList.FindIndex(finddata => finddata.CraftItem3 == data.CraftItem2);
                    if (itemIndex != -1)
                    {
                         addCount2 = FindCount(DataList, itemIndex);
                    }
               }
               data.CraftItemCount = BigInteger.Max(addCount1, addCount2) + 1; //將計算結果存回到data中
               return data.CraftItemCount;
          }

          private void btnFind_Click(object sender, EventArgs e)
          {
               lvFindResult.Clear();
               string FindTarget = txtTargetWord.Text.Trim();
               if (string.IsNullOrWhiteSpace(FindTarget))
               {
                    MessageBox.Show("Please enter a word to find.");
                    return;
               }
               List<string> ShowStrs = new List<string>();
               FindTargetFormula(FindTarget, null, ShowStrs);
               if (ShowStrs.Count == 0)
               {
                    lvFindResult.Items.Add("No results found.");
               }
               else
               {
                    foreach (var str in ShowStrs)
                    {
                         lvFindResult.Items.Add(str);
                    }
               }
          }

          private void FindTargetFormula(string TargetItem3, List<string> FindedItems, List<string> ShowStrs)
          {
               if (FindedItems == null)
               {
                    FindedItems = new List<string>();
                    FindedItems.Add(TargetItem3);
               }
               else
               {
                    if (FindedItems.FindIndex(find => find == TargetItem3) != -1)
                    {
                         return;
                    }
                    else
                    {
                         FindedItems.Add(TargetItem3);
                    }
               }

               //從FileDatas中找到TargetItem3的CraftItem1和CraftItem2，然後組合成公式
               var targetData = FileDatas.FirstOrDefault(data => data.CraftItem3 == TargetItem3);
               if (targetData == null)
               {
                    return;
               }
               FindTargetFormula(targetData.CraftItem1.ToString(), FindedItems, ShowStrs);
               FindTargetFormula(targetData.CraftItem2.ToString(), FindedItems, ShowStrs);
               StringBuilder formula = new StringBuilder();
               if (!string.IsNullOrWhiteSpace(targetData.CraftItem1))
               {
                    formula.Append(targetData.CraftItem1_emoji + " " + targetData.CraftItem1 + targetData.Hist1);
               }
               if (!string.IsNullOrWhiteSpace(targetData.CraftItem2))
               {
                    formula.Append(targetData.CraftItem2_emoji + " " + targetData.CraftItem2 + targetData.Hist2);
               }
               formula.Append(targetData.CraftItem3_emoji + " " + targetData.CraftItem3);
               ShowStrs.Add(formula.ToString());
          }

          private void txtTargetWord_KeyDown(object sender, KeyEventArgs e)
          {
               if (e.KeyCode == Keys.Enter)
               {
                    btnFind.PerformClick();
               }
          }

          private int BlurFind_StartIndex = 0;
          private int BlurFind_EndIndex = 0;
          private int LastFindLength = 0;
          private void txtBlurFind_TextChanged(object sender, EventArgs e)
          {
               string BlurText = txtBlurFind.Text.Trim();
               if (string.IsNullOrWhiteSpace(BlurText))
               {
                    BlurFind_StartIndex = 0;
                    BlurFind_EndIndex = FileDatas.Count - 1;
                    return;
               }
               if (BlurText.Length > LastFindLength)
               {
                    BlurFind_EndIndex = FileDatas.Count - 1;
               }
               LastFindLength = BlurText.Length;
               bool FirstFind = false;
               for (int index = BlurFind_StartIndex; index <= BlurFind_EndIndex; index++)
               {
                    if (index >= FileDatas.Count) break;
                    if (FileDatas[index].CraftItem3.Contains(BlurText))
                    {
                         if (FirstFind == false)
                         {
                              BlurFind_StartIndex = index;
                              FirstFind = true;
                         }
                    }
                    if (FirstFind == true && FileDatas[index].CraftItem3.Contains(BlurText) == false)
                    {
                         BlurFind_EndIndex = index - 1;
                    }
               }
               lvFindResult.Clear();
               for (int index = BlurFind_StartIndex; index <= BlurFind_EndIndex; index++)
               {
                    lvFindResult.Items.Add($"{FileDatas[index].CraftItem3_emoji} {FileDatas[index].CraftItem3}");
               }
          }
          private void TextBox_ClickToSelectAll(object sender, EventArgs e)
          {
               var textBox = sender as TextBox;
               textBox.BeginInvoke(new Action(textBox.SelectAll));
          }

          private void btnReload_Click(object sender, EventArgs e)
          {
               LoadDataFromFile();
          }
     }

     public class DictionaryData
     {
          public string CraftItem1_emoji { get; set; }
          public string CraftItem2_emoji { get; set; }
          public string CraftItem3_emoji { get; set; }

          public string CraftItem1 { get; set; }
          public string CraftItem2 { get; set; }
          public string CraftItem3 { get; set; }
          public string Hist1 { get; set; }
          public string Hist2 { get; set; }
          //合出Item3經過幾個等號
          public BigInteger CraftItemCount { get; set; }
     }

}

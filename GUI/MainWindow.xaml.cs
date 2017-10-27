using OfficeOpenXml;
using SC.API.ComInterop;
using SC.API.ComInterop.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void download_Click(object sender, RoutedEventArgs e)
        {
            string fileLocation = System.IO.Directory.GetParent
              (System.IO.Directory.GetParent(Environment.CurrentDirectory)
              .ToString()).ToString();
            string user = userBox.Text;
            string pass = passBox.Password.ToString();
            string url = urlBox.Text;
            MatchCollection matchUrl = Regex.Matches(url, @"story\/(.+)\/view");
            string[] matchGroup = null;
            string storyUrl = "";
            if (matchUrl.Count > 0)
            {
                matchGroup = matchUrl[0].ToString().Split('/');
                storyUrl = matchGroup[2];
            }
            else
            {
                storyUrl = url;
            }

            SharpCloudApi sc = null;
            Story story = null;
            try
            {
                sc = new SharpCloudApi(user, pass);
                Console.WriteLine(sc.StoriesMy());

            }
            catch
            {
                MessageBox.Show("Invalid Username or Password");
            }

            try
            {
                story = sc.LoadStory(storyUrl);
            }
            catch
            {
                MessageBox.Show("Cannot Load Story");
            }
            if(story != null)
            {
                var roadSplit = story.Name.Split('(');

                var roadName = "";
                if (roadSplit.Length > 0)
                    roadName = roadSplit[0];
                else
                    roadName = story.Name;

                FileInfo newFile = new FileInfo(fileLocation + "\\" + roadName + "(" + DateTime.Now.Month + DateTime.Now.Day + DateTime.Now.Year 
                    + ")" + "(" + DateTime.Now.Hour + DateTime.Now.Minute + DateTime.Now.Second + ")" + ".xlsx");
                ExcelPackage pck = new ExcelPackage(newFile);
                var currentStory = sc.LoadStory(story.Id);
                var itemSheet = pck.Workbook.Worksheets.FirstOrDefault(x => x.Name == "Items");
                if (itemSheet == null)
                {
                    itemSheet = pck.Workbook.Worksheets.Add("Items");
                }
                var relationshipSheet = pck.Workbook.Worksheets.First();
                if (pck.Workbook.Worksheets.Count > 1)
                    relationshipSheet = pck.Workbook.Worksheets.ElementAt(1);
                if (relationshipSheet == itemSheet)
                {
                    relationshipSheet = pck.Workbook.Worksheets.Add("RelationshipSheet");
                }
                var headList = new List<string> { "Name", "Description", "Category", "Start", "Duration" };
                // Filters the default attributes from the story
                var attData = currentStory.Attributes;
                var attList = new List<SC.API.ComInterop.Models.Attribute>();
                Regex regex = new Regex(@"none|None|Sample");
                var attCount = 0;
                foreach (var att in attData)
                {
                    // Checks to see if attribute header is a default attritube.
                    Match match = regex.Match(att.Name);
                    if (!match.Success)
                    {
                        // Adds non-default attribute to the List and to the header line
                        attList.Add(att);
                        attCount++;
                        headList.Add(att.Name + "|" + att.Type + "|" + att.Description);
                    }
                }
                var go = 1;
                foreach (var head in headList)
                {
                    itemSheet.Cells[1, go].Value = head;
                    go++;
                }
                ItemSheet(currentStory, sc, attList, attCount, itemSheet);
                RelationshipSheet(currentStory, relationshipSheet);
                pck.SaveAs(newFile);
                MessageBox.Show("Download Complete");
            }
        }
        private static void RelationshipSheet(Story Story, OfficeOpenXml.ExcelWorksheet relationshipSheet)
        {
            // file path variable
            // Header Line
            relationshipSheet.Cells["A1"].Value = "Item 1";
            relationshipSheet.Cells["B1"].Value = "Item 2";
            relationshipSheet.Cells["C1"].Value = "Direction";
            relationshipSheet.Cells["D1"].Value = "Tags";
            relationshipSheet.Cells["E1"].Value = "Comment";
            var goAtt = 6;
            foreach (var att in Story.RelationshipAttributes)
            {
                relationshipSheet.Cells[1, goAtt].Value = att.Name + "|" + att.Type;
                goAtt++;
            }
            var count = 2;
            // Parse through relationship data
            foreach (var line in Story.Relationships)
            {
                relationshipSheet.Cells[count, 1].Value = line.Item1.Name;
                relationshipSheet.Cells[count, 2].Value = line.Item2.Name;
                relationshipSheet.Cells[count, 3].Value = line.Direction.ToString();
                var tagLine = "";
                foreach (var lineTag in line.Tags)
                {
                    tagLine += lineTag.Text + "|";
                }
                relationshipSheet.Cells[count, 4].Value = tagLine;
                relationshipSheet.Cells[count, 5].Value = line.Comment;
                var go = 6;
                foreach (var att in Story.RelationshipAttributes)
                {
                    relationshipSheet.Cells[count, go].Value = line.GetAttributeValueAsText(Story.RelationshipAttribute_FindByName(att.Name));
                    go++;
                }
                count++;
            }
            //Write data to file
            Console.WriteLine("RelationshipSheet written");
        }
        private static void ItemSheet(Story story, SharpCloudApi sc, List<SC.API.ComInterop.Models.Attribute> attList, int attCount, ExcelWorksheet sheet)
        {
            int sheetLine = 2;
            var fileLocation = System.IO.Directory.GetParent
                (System.IO.Directory.GetParent(Environment.CurrentDirectory)
                .ToString()).ToString();
            var catData = story.Categories;
            var itemSheet = sheet;
            // Goes through items in category order
            foreach (var cat in catData)
            {
                foreach (var item in story.Items)
                {
                    // check to see if category matches item category
                    if (item.Category.Name == cat.Name)
                    {
                        // Creates the initial list for the item 
                        var itemList = new List<string> { item.Name, item.Description, item.Category.Name, item.StartDate.ToString(), item.DurationInDays.ToString() };
                        // adds the sub category to the item
                        string[] itemLine = itemList.ToArray();
                        var go = 1;
                        foreach (var itemCell in itemLine)
                        {
                            itemSheet.Cells[sheetLine, go].Value = itemCell;
                            if (go == 5)
                            {
                                itemSheet.Cells[sheetLine, go].Value = Double.Parse(itemCell);
                                itemSheet.Cells[sheetLine, go].Style.Numberformat.Format = "#";
                            }
                            if (go == 10)
                            {
                                itemSheet.Cells[sheetLine, go].Value = Double.Parse(itemCell);
                            }

                            go++;
                        }
                        // Adds the attributes to the item
                        foreach (var att in attList)
                        {
                            switch (att.Type.ToString())
                            {
                                case "Text":
                                    itemSheet.Cells[sheetLine, go].Value = item.GetAttributeValueAsText(att);
                                    break;
                                case "Numeric":
                                    itemSheet.Cells[sheetLine, go].Value = item.GetAttributeValueAsDouble(att);
                                    itemSheet.Cells[sheetLine, go].Style.Numberformat.Format = "0.00";
                                    break;
                                case "Date":
                                    itemSheet.Cells[sheetLine, go].Value = item.GetAttributeValueAsDate(att);
                                    itemSheet.Cells[sheetLine, go].Style.Numberformat.Format = "mm/dd/yyyy hh:mm:ss AM/PM";
                                    break;
                                case "List":
                                    itemSheet.Cells[sheetLine, go].Value = item.GetAttributeValueAsText(att);
                                    break;
                                case "Location":
                                    itemSheet.Cells[sheetLine, go].Value = item.GetAttributeValueAsText(att);
                                    break;
                            }
                            go++;
                        }
                        // Adds entire list to the row for the item.

                        sheetLine++;
                    }
                }
            }
            // Writes file to disk
            Console.WriteLine("ItemSheet Written");
        }
    }
}

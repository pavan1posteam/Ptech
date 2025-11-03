using Newtonsoft.Json;
using PTech;
using Microsoft.VisualBasic.FileIO;
using PTech.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Ptech.Models;

namespace Ptech
{
    class Scotch
    {
        string DeveloperId = ConfigurationManager.AppSettings["DeveloperId"];
        string folderPath = ConfigurationManager.AppSettings.Get("BaseDirectory");
        string PostiveQty = ConfigurationManager.AppSettings["PostiveQty"];
        string StaticQty = ConfigurationManager.AppSettings["Qty"];

        public readonly int StoreId;
        public readonly decimal Tax;
        public string BaseUrl;
        public readonly string posstoreId;
        public string Apikey;
        public Scotch(int _storeid, decimal _tax, string _baseurl, string _posstoreId, string accesstoken)
        {
            StoreId = _storeid;
            Tax = _tax;
            BaseUrl = _baseurl;
            posstoreId = _posstoreId;
            Apikey = accesstoken;
            
        }
        public async Task RunAsync()
        {
            try
            {
                string url = await getfile();
                if (!string.IsNullOrEmpty(url))
                {
                    await downloading(url);
                }
                clsConvertRawFile(StoreId, 0);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public async Task<string> getfile()
        {
            try
            {
                BaseUrl += "/partners/v1/"+posstoreId+"/inventory/sync_file";
                var client = new RestClient(BaseUrl);
                var request = new RestRequest("", Method.GET);
                request.AddHeader("Authorization", "Bearer " + Apikey);
                request.AddHeader("Accept", "application/json");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var response = client.Execute(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var linkData = JsonConvert.DeserializeObject<getfileformat>(response.Content);
                    return linkData.url;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return "";
        }
        public async Task downloading(string url)
        {
            using (var client = new HttpClient())
            {
                Console.WriteLine("Downloading file...");

                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();
                var contentDisposition = response.Content.Headers.ContentDisposition;
                string fileName = null;

                if (contentDisposition != null)
                {
                    fileName = !string.IsNullOrEmpty(contentDisposition.FileNameStar)
                        ? contentDisposition.FileNameStar
                        : contentDisposition.FileName?.Trim('"');
                }

                if (string.IsNullOrEmpty(fileName))
                    fileName = StoreId + "_Data.csv";
                string path = folderPath;
                path += "\\"+StoreId + "\\Raw" ;
                Directory.CreateDirectory(path);
                string filePath = Path.Combine(path, fileName);

                using (var contentStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await contentStream.CopyToAsync(fileStream);
                }
                Console.WriteLine($"Download complete → {filePath}");
            }
        }
        public static DataTable ConvertCsvToDataTable(string FileName)
        {
            DataTable dtResult = new DataTable();
            try
            {
                using (TextFieldParser parser = new TextFieldParser(FileName))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.Delimiters = new string[] { "," };
                    parser.HasFieldsEnclosedInQuotes = true;

                    string[] columns = parser.ReadFields();

                    for (int i = 0; i < columns.Length; i++)
                    {
                        dtResult.Columns.Add(columns[i], typeof(string));
                    }

                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        DataRow newrow = dtResult.NewRow();
                        for (int i = 0; i < fields.Length; i++)
                        {
                            if (dtResult.Columns.Count != fields.Length)
                            {
                                break;
                            }
                            newrow[i] = fields[i];
                        }
                        dtResult.Rows.Add(newrow);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return dtResult;
        }
        public string clsConvertRawFile(int StoreId, decimal Tax)
        {
            string BaseUrl = ConfigurationManager.AppSettings.Get("BaseDirectory");
            if (Directory.Exists(BaseUrl))
            {
                if (Directory.Exists(BaseUrl + "/" + StoreId + "/Raw/"))
                {
                    var directory = new DirectoryInfo(BaseUrl + "/" + StoreId + "/Raw/");
                    if (directory.GetFiles().FirstOrDefault() != null)
                    {
                        var myFile = (from f in directory.GetFiles()
                                      orderby f.LastWriteTime descending
                                      select f).First();

                        string Url = BaseUrl + "/" + StoreId + "/Raw/" + myFile;
                        if (File.Exists(Url))
                        {
                            try
                            {
                                DataTable dt = ConvertCsvToDataTable(Url);

                                var dtr = from s in dt.AsEnumerable() select s;
                                List<ProductModel> prodlist = new List<ProductModel>();
                                List<FullNameProductModel> full = new List<FullNameProductModel>();
                                foreach (DataRow dr in dt.Rows)
                                {
                                    ProductModel pmsk = new ProductModel();
                                    FullNameProductModel fname = new FullNameProductModel();
                                    Verify v = new Verify(dr, StoreId);
                                    
                                    pmsk.StoreID = StoreId;
                                    string[] upcs = v.GetStringByIndex(0).Split('|');
                                    if (!string.IsNullOrEmpty(upcs[0]))
                                    {
                                        pmsk.upc = "#" + upcs[0];
                                        fname.upc = pmsk.upc;
                                        pmsk.sku = pmsk.upc;
                                        fname.sku = pmsk.upc;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                    for(int i =1; i < upcs.Length; i++)
                                    {
                                        if (i == 1)
                                            pmsk.altupc1 = "#" + upcs[i];
                                        if (i == 2)
                                            pmsk.altupc2 = "#" + upcs[i];
                                        if (i == 3)
                                            pmsk.altupc3 = "#" + upcs[i];
                                        if (i == 4)
                                            pmsk.altupc4 = "#" + upcs[i];
                                        if (i == 5)
                                            pmsk.altupc5 = "#" + upcs[i];
                                    }
                                    if (prodlist.Any(x => x.sku == pmsk.sku))
                                        continue;
                                    pmsk.StoreProductName = v.GetStringByIndex(2);
                                    pmsk.StoreDescription = pmsk.StoreProductName;
                                    fname.pdesc = pmsk.StoreProductName;
                                    fname.pname = pmsk.StoreProductName;
                                    pmsk.uom = v.GetStringByIndex(3);
                                    fname.uom = pmsk.uom;
                                    pmsk.Price = Convert.ToDecimal(v.GetDecimalByIndex(4).ToString().Replace("$", ""));
                                    fname.Price = pmsk.Price;

                                    pmsk.pack = v.getpack(pmsk.StoreProductName);
                                    pmsk.Tax = Tax;
                                    pmsk.Qty = v.GetIntByIndex(5);

                                    pmsk.Start = "";
                                    pmsk.End = "";
                                    fname.pcat = v.GetStringByIndex(9);
                                    fname.pcat1 = v.GetStringByIndex(8);
                                    fname.pcat2 = "";
                                    fname.region = "";
                                    fname.country = "";
                                    pmsk.Vintage = v.GetStringByIndex(6);
                                    if (pmsk.Price > 0)
                                    {
                                        prodlist.Add(pmsk);
                                        full.Add(fname);
                                    }
                                }

                                Console.WriteLine("Generating " + StoreId + " CSV Files.....");
                                string filename = GenerateCSV.GenerateCSVFile(prodlist, "PRODUCT", StoreId, BaseUrl);
                                filename = GenerateCSV.GenerateCSVFile(full, "FULLNAME", StoreId, BaseUrl);
                                Console.WriteLine("File Generated For Scotch " + StoreId);
                                string[] filePaths = Directory.GetFiles(BaseUrl + "/" + StoreId + "/Raw/");
                                string path = folderPath;
                                foreach (string filePath in filePaths)
                                {
                                    path += "\\" + StoreId + "\\RawDeleted";
                                    Directory.CreateDirectory(path);
                                    string destpath = filePath.Replace(@"/Raw/", @"/RawDeleted/" + DateTime.Now.ToString("yyyyMMddhhmmss"));
                                    File.Move(filePath, destpath);
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.Message);
                                (new clsEmail()).sendEmail(DeveloperId, "", "", "Error in ExtractPOS@" + StoreId + DateTime.UtcNow + " GMT", e.Message + "<br/>" + e.StackTrace);
                                return "Not generated file for NCRCounterpoint " + StoreId;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid FileName or Raw Folder is Empty! " + StoreId);
                        return "";
                    }
                }
                else
                {
                    return "Invalid Sub-Directory " + StoreId;
                }
            }
            else
            {
                return "Invalid Directory " + StoreId;
            }
            return "Completed generating File";
        }
    }
    public class getfileformat
    {
        public string generated_at { get; set; }
        public string url { get; set; }
    }

}

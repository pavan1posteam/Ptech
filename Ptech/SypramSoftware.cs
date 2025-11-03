using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ptech.Models;
using PTech;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static PTech.clsPTECH;

namespace Ptech
{
    class SypramSoftware
    {
        string BaseUrl = ConfigurationManager.AppSettings.Get("BaseDirectory");
        string DeveloperId = ConfigurationManager.AppSettings["DeveloperId"];
        public SypramSoftware(int storeid, decimal tax, string BaseUrl, string Username, string Password, string Pin, bool IsMarkUpPrice, int MarkUpValue)
        {
            SypramForCSV(storeid, tax, BaseUrl, Username, Password, Pin, IsMarkUpPrice, MarkUpValue);

        }
        public List<SypramDatum> products(int StoreId, decimal tax, string BaseUrl, string Username, string Password, string Pin)
        {
            List<SypramDatum> productList = new List<SypramDatum>();
            try
            {
                string authInfo = Username + ":" + Password + ":" + Pin;
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                string content = null;
                PTECHclsProductList obj = new PTECHclsProductList();
                BaseUrl = string.IsNullOrEmpty(obj.Url) ? BaseUrl : obj.Url;
                var client = new RestClient(BaseUrl);
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", "Basic " + authInfo);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("Accept", "application/json");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                IRestResponse response = client.Execute(request);
                content = response.Content;
                if (content == "Unauthorized" || content == "" || content == "null")
                {
                    (new clsEmail()).sendEmail(DeveloperId, "", "", "Error in " + StoreId + " Ptech Pos@" + DateTime.UtcNow + " GMT", " ERROR  In Response while reteriving Products " + ":" + response.StatusCode);

                }
                var result = JsonConvert.DeserializeObject<clsProductList.items>(content);
                var pJson = (dynamic)JObject.Parse(content);
                var jArray = (JArray)pJson["Data"];
                

                var settings = new JsonSerializerSettings
                {
                    MetadataPropertyHandling = MetadataPropertyHandling.Ignore
                };
                SypramRoot ResponseData = JsonConvert.DeserializeObject<SypramRoot>(content, settings);
                productList = ResponseData.Data;
                //File.AppendAllText("11714.json", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return productList;
        }
        public void SypramForCSV(int storeid, decimal tax, string BaseUrl, string Username, string Password, string Pin, bool IsMarkUpPrice, int MarkUpValue)
        {
            string DeveloperId = ConfigurationManager.AppSettings["DeveloperId"];
            string PriceBstore = ConfigurationManager.AppSettings["PriceBstore"];
            string Small_Case = ConfigurationManager.AppSettings["Small_Case"];
            string IncludePack = ConfigurationManager.AppSettings["IncludePack"];
            try
            {
                var productList = products(storeid, tax, BaseUrl, Username, Password, Pin);

                List<datatableModel> pf = new List<datatableModel>();
                string qty = ConfigurationManager.AppSettings.Get("Qty");
                string Tax = ConfigurationManager.AppSettings.Get("Tax");
                if (productList != null)
                {
                    if (productList.Count > 0)
                    {
                        foreach (var item in productList)
                        {
                            datatableModel pdf = new datatableModel();
                            pdf.StoreID = storeid;
                            if (string.IsNullOrEmpty(item.upc))
                                pdf.upc = "";
                            else
                                pdf.upc = item.upc;
                            pdf.sku = item.sku;
                            if (qty.Contains(storeid.ToString()))
                                pdf.Qty = 99;
                            else
                                pdf.Qty = item.totalqty;
                            string pack = string.IsNullOrEmpty(item.packname) ? "" : item.packname;
                            pack = Regex.Replace(pack, "[^0-9]", String.Empty);
                            if (IncludePack.Contains(storeid.ToString()))
                            {
                                if (string.IsNullOrEmpty(pack))
                                    pdf.pack = "1";
                                else
                                    pdf.pack = getpack(item.packname).ToString();
                            }
                            else if (!string.IsNullOrEmpty(pack))
                                pdf.pack = pack;
                            else
                                pdf.pack = "1";
                            pdf.uom = string.IsNullOrEmpty(item.sizename) ? "": item.sizename;
                            pdf.StoreProductName = string.IsNullOrEmpty(item.itemname) ? "" : item.itemname;
                            pdf.StoreDescription = pdf.StoreProductName;
                            decimal price;
                            if (IsMarkUpPrice)
                            {
                                price = Convert.ToDecimal(item.price);
                                decimal markup = price * MarkUpValue / 100 + price;
                                pdf.Price = (markup);
                                pdf.Price = Decimal.Round(pdf.Price, 2);
                            }
                            else
                            {
                                if (PriceBstore.Contains(storeid.ToString())) //regarding ticket #21616
                                    pdf.Price = Convert.ToDecimal(item.price);
                                else
                                    pdf.Price = Convert.ToDecimal(item.price);
                            }
                            pdf.sprice = Convert.ToDecimal(item.saleprice);
                            pdf.Start = "";
                            pdf.End = "";
                            if (Tax.Contains(storeid.ToString()))//11039
                            {
                                if (pdf.pcat == "BEVERAGES" || pdf.pcat == "MIXERS" || pdf.pcat == "SNACKS")
                                    pdf.Tax = Convert.ToDecimal(0.04);
                                else
                                    pdf.Tax = Convert.ToDecimal(0.08);
                            }
                            else
                                pdf.Tax = tax;
                            pdf.altupc1 = "";
                            pdf.altupc2 = "";
                            pdf.altupc3 = "";
                            pdf.altupc4 = "";
                            pdf.altupc4 = "";
                            pdf.altupc5 = "";
                            pdf.pcat = string.IsNullOrEmpty(item.department) ? "" : item.department;
                            pdf.Cost = item.cost;
                            if (pdf.Price > 0)
                            {
                                pf.Add(pdf);
                            }
                        }
                        Datatabletocsv csv = new Datatabletocsv();
                        csv.Datatablecsv(storeid, tax, pf, IsMarkUpPrice, MarkUpValue);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "-" +storeid);
                (new clsEmail()).sendEmail(DeveloperId, "", "", "Error in " + storeid + " Ptech@" + DateTime.UtcNow + " GMT", "StatusCode:ERROR Response2" + "<br/>" + ex.Message + "<br/>" + ex.StackTrace);
            }
        }

        public int getpack(string pack)
        {
            pack = pack.ToUpper();
            var regexMatch = Regex.Match(pack, @"(?<Result>\d+)PK | (?<Result>\d+)-PACK");
            var prodPack = regexMatch.Groups["Result"].Value;
            if (prodPack.Length > 0)
            {
                return ParseIntValue(prodPack);
            }
            return 1;
        }
        public int ParseIntValue(string val)
        {
            int outVal = 0;
            int.TryParse(val.Replace("$", ""), out outVal);
            return outVal;
        }
    }
    public class SypramDatum
    {
        public string upc { get; set; }
        public string sku { get; set; }
        public string itemname { get; set; }
        public decimal price { get; set; }
        public decimal cost { get; set; }
        public double saleprice { get; set; }
        public string sizename { get; set; }
        public string packname { get; set; }
        public string vintage { get; set; }
        public string department { get; set; }
        public double pricea { get; set; }
        public double priceb { get; set; }
        public double pricec { get; set; }
        public decimal totalqty { get; set; }
        public object altupc1 { get; set; }
        public object altupc2 { get; set; }
        public string storecode { get; set; }
    }

    public class SypramRoot
    {
        public bool StatusVal { get; set; }
        public int StatusCode { get; set; }
        public string StatusMsg { get; set; }
        public string SessionID { get; set; }
        public List<SypramDatum> Data { get; set; }
        public object ExtraData { get; set; }
    }


}

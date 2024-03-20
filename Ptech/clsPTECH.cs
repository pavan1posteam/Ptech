using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PTech.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace PTech
{
    class clsPTECH
    {
        string DeveloperId = ConfigurationManager.AppSettings["DeveloperId"];
        string PriceBstore = ConfigurationManager.AppSettings["PriceBstore"];
        public List<JArray> products(int StoreId, decimal tax, string BaseUrl, string Username, string Password, string Pin)
        {
            List<JArray> productList = new List<JArray>();
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
                productList.Add(jArray);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return productList;
        }

        public class PtechPos
        {
            public List<Datum> Products(int StoreId, decimal tax, string BaseUrl, string Username, string Password)
            {
                List<Datum> productList = new List<Datum>();

                try
                {
                    var client = new RestClient(BaseUrl);
                    client.Timeout = -1;
                    var request = new RestRequest(Method.GET);
                    request.AddHeader("UserId", Username);
                    request.AddHeader("Password", Password);
                    request.AddHeader("Content-Type", "application/json");
                    var body = @"{
                       " + "\n" +
                    @" ""PositiveOnly"" : true,
                          " + "\n" +
                    @" ""WithNegative"" : false
                                   " + "\n" +
                             @"}";
                    request.AddParameter("application/json", body, ParameterType.RequestBody);
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    IRestResponse response = client.Execute(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var content = response.Content;
                        var jsons = JsonConvert.DeserializeObject<Root>(content);
                        var model = jsons.Data;
                        productList = model;
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "PTECH");
                }
                return productList;
            }
            public PtechPos(int storeid, decimal tax, string BaseUrl, string Username, string Password, bool IsMarkUpPrice, int MarkUpValue) // MarkUp Added as per ticket 15542 Store 11446
            {
                PtechcsvConverter(storeid, tax, BaseUrl, Username, Password, IsMarkUpPrice, MarkUpValue);
            }
            public void PtechcsvConverter(int storeid, decimal tax, string BaseUrl, string Username, string Password, bool IsMarkUpPrice, int MarkUpValue)
            {
                string DeveloperId = ConfigurationManager.AppSettings["DeveloperId"];
                string Sale_Price = ConfigurationManager.AppSettings["Sale_Price"];
                string different_price = ConfigurationManager.AppSettings["different_price"];
                var productList = Products(storeid, tax, BaseUrl, Username, Password);
                try
                {
                    string folderPath = ConfigurationManager.AppSettings.Get("BaseDirectory");
                    List<datatableModel> pf = new List<datatableModel>();
                    foreach (var item in productList)
                    {
                        datatableModel pdf = new datatableModel();
                        pdf.StoreID = storeid;
                        pdf.upc = item.UPC;
                        string abc = pdf.upc;
                        abc = Regex.Replace(abc, "[^0-9]", String.Empty);
                        pdf.upc = '#' + abc;
                        pdf.sku = '#' + item.SKU.ToString();
                        pdf.Qty = Convert.ToInt32(item.QTY);
                        pdf.pack = item.PACK;
                        string str = item.PACK;
                        str = Regex.Replace(str, "[^0-9]", String.Empty);
                        pdf.pack = str;
                        pdf.uom = item.SIZE;
                        string b = item.ITEMNAME;
                        if (b.Contains("\n"))
                        {
                            b = b.Replace("\n", String.Empty);
                        }

                        pdf.StoreProductName = b;
                        string a = item.ITEMNAME;
                        if (a.Contains("\n"))
                        {
                            a = a.Replace("\n", String.Empty);
                        }
                        pdf.StoreDescription = a.Trim();
                        if (different_price.Contains(storeid.ToString()))
                        {
                            pdf.Price = Convert.ToDecimal(item.PRICEA);
                        }
                        else
                        {
                            pdf.Price = Convert.ToDecimal(item.REGPRICE);
                        }
                        if (Sale_Price.Contains(storeid.ToString()))
                        {
                            pdf.sprice = Convert.ToDecimal(item.PRICEA);
                        }
                        pdf.Start = "";
                        pdf.End = "";
                        pdf.Tax = tax;
                        pdf.pcat = item.DEPNAME;
                        string c = item.CATNAME;
                        if (storeid == 11683 && pdf.pcat.Contains("A-LIQUOR") && c.Contains("Allocated BBN"))
                        {
                            continue;
                        }

                        pdf.altupc1 = "";
                        pdf.altupc2 = "";
                        pdf.altupc3 = "";
                        pdf.altupc4 = "";

                        pf.Add(pdf);
                    }
                    Datatabletocsv csv = new Datatabletocsv();
                    csv.Datatablecsv(storeid, tax, pf, IsMarkUpPrice, MarkUpValue);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    new clsEmail().sendEmail(DeveloperId, "", "", "Error in PTECHPOS@" + DateTime.UtcNow + " GMT", ex.Message + "<br/>" + ex.StackTrace);
                }
            }
        }
        public class PTECHCsvProducts
        {
            string BaseUrl = ConfigurationManager.AppSettings.Get("BaseDirectory");

            public PTECHCsvProducts(int storeid, decimal tax, string BaseUrl, string Username, string Password, string Pin, bool IsMarkUpPrice, int MarkUpValue)
            {
                PtechForCSV(storeid, tax, BaseUrl, Username, Password, Pin, IsMarkUpPrice, MarkUpValue);

            }
            public void PtechForCSV(int storeid, decimal tax, string BaseUrl, string Username, string Password, string Pin, bool IsMarkUpPrice, int MarkUpValue)
            {
                string DeveloperId = ConfigurationManager.AppSettings["DeveloperId"];
                string PriceBstore = ConfigurationManager.AppSettings["PriceBstore"];
                try
                {
                    clsPTECH products = new clsPTECH();
                    var productList = products.products(storeid, tax, BaseUrl, Username, Password, Pin);

                    List<datatableModel> pf = new List<datatableModel>();
                    string qty = ConfigurationManager.AppSettings.Get("Qty");
                    string Tax = ConfigurationManager.AppSettings.Get("Tax");
                    if (productList != null)
                    {
                        if (productList.Count > 0)
                        {
                            foreach (var item in productList)
                            {
                                foreach (var itm in item)
                                {
                                    datatableModel pdf = new datatableModel();

                                    pdf.StoreID = storeid;

                                    decimal result;
                                    string upc = itm["UPC"].ToString();
                                    Decimal.TryParse(upc, System.Globalization.NumberStyles.Float, null, out result);
                                    upc = result.ToString();

                                    if (upc == "" || upc == "0")
                                    {
                                        pdf.upc = "";
                                    }
                                    else
                                    {
                                        pdf.upc = upc;
                                    }

                                    string sku = itm["SKU"].ToString();

                                    pdf.sku = sku;
                                    if (qty.Contains(storeid.ToString()))         //11287
                                    {
                                        pdf.Qty = 99;
                                    }
                                    else
                                    {
                                        //pdf.Qty = Convert.ToInt32(itm["TotalQty"]);
                                        pdf.Qty = Convert.ToInt32(itm["TotalQty"]) >= 0 ? Convert.ToInt32(itm["TotalQty"]) : 0;
                                    }

                                    pdf.pack = 1.ToString();
                                    pdf.StoreProductName = itm["ItemName"].ToString();
                                    pdf.StoreDescription = itm["ItemName"].ToString();

                                    decimal price;

                                    if (IsMarkUpPrice)
                                    {
                                        price = Convert.ToDecimal(itm["Price"]);
                                        decimal markup = price * MarkUpValue / 100 + price;
                                        pdf.Price = (markup);
                                        pdf.Price = Decimal.Round(pdf.Price, 2);
                                    }
                                    else
                                    {
                                        if (PriceBstore.Contains(storeid.ToString())) //regarding ticket #21616
                                        {
                                            pdf.Price = Convert.ToDecimal(itm["PriceB"]);
                                        }
                                        else
                                        {
                                            pdf.Price = Convert.ToDecimal(itm["Price"]);
                                        }
                                    }
                                    pdf.sprice = Convert.ToDecimal(itm["SALEPRICE"]);
                                    pdf.Start = "";
                                    pdf.End = "";
                                    pdf.Tax = tax;
                                    pdf.altupc1 = "";
                                    pdf.altupc2 = "";
                                    pdf.altupc3 = "";
                                    pdf.altupc4 = "";
                                    pdf.altupc4 = "";
                                    pdf.altupc5 = "";
                                    pdf.uom = itm["SizeName"].ToString();
                                    pdf.pcat = itm["Department"].ToString();
                                    if (Tax.Contains(storeid.ToString()))           //11039
                                    {
                                        if (pdf.pcat == "BEVERAGES" || pdf.pcat == "MIXERS" || pdf.pcat == "SNACKS")
                                        {
                                            pdf.Tax = Convert.ToDecimal(0.04);
                                        }
                                        else
                                        {
                                            pdf.Tax = Convert.ToDecimal(0.08);
                                        }
                                    }
                                    else
                                    {
                                        pdf.Tax = tax;
                                    }

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
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + storeid);
                    (new clsEmail()).sendEmail(DeveloperId, "", "", "Error in " + storeid + " Ptech@" + DateTime.UtcNow + " GMT", "StatusCode:ERROR Response2" + "<br/>" + ex.Message + "<br/>" + ex.StackTrace);
                }
            }

        }
        public class Datum
        {
            public int SKU { get; set; }
            public string UPC { get; set; }
            public string ITEMNAME { get; set; }
            public double REGPRICE { get; set; }
            public double PRICEA { get; set; }
            public double PRICEB { get; set; }
            public double PRICEC { get; set; }
            public string SIZE { get; set; }
            public string PACK { get; set; }
            public string DEPNAME { get; set; }
            public string CATNAME { get; set; }
            public double QTY { get; set; }
            public int UNIQNO { get; set; }
        }
        public class Root
        {
            public bool StatusVal { get; set; }
            public int StatusCode { get; set; }
            public string StatusMsg { get; set; }
            public string SessionID { get; set; }
            public List<Datum> Data { get; set; }
        }
        public class clsProductList
        {
            public bool StatusVal { get; set; }
            public int StatusCode { get; set; }
            public string StatusMsg { get; set; }
            public string Price { get; set; }
            public string SessionID { get; set; }

            public string Url { get; set; }
            public class Data
            {
                public string UPC { get; set; }
                public string SKU { get; set; }
                public string ItemName { get; set; }
                public decimal Price { get; set; }
                public decimal Cost { get; set; }
                public decimal SALEPRICE { get; set; }
                public string SizeName { get; set; }
                public string PackName { get; set; }
                public string Vintage { get; set; }
                public string Department { get; set; }
                public decimal PriceA { get; set; }
                public decimal PriceB { get; set; }
                public decimal PriceC { get; set; }
                public Int32 TotalQty { get; set; }
                public decimal tax { get; set; }
            }
            public class items
            {
                public List<Data> item { get; set; }
            }
        }
        public class PTECHclsProductList
        {
            public bool StatusVal { get; set; }
            public int StatusCode { get; set; }
            public string StatusMsg { get; set; }
            public string Price { get; set; }
            public string SessionID { get; set; }

            public string Url { get; set; }
            public class Data
            {
                public string UPC { get; set; }
                public string SKU { get; set; }
                public string ItemName { get; set; }
                public decimal Price { get; set; }
                public decimal Cost { get; set; }
                public decimal SALEPRICE { get; set; }
                public string SizeName { get; set; }
                public string PackName { get; set; }
                public string Vintage { get; set; }
                public string Department { get; set; }
                public decimal PriceA { get; set; }
                public decimal PriceB { get; set; }
                public decimal PriceC { get; set; }
                public Int32 TotalQty { get; set; }
                public decimal tax { get; set; }
            }
            public class items
            {
                public List<Data> item { get; set; }
            }
        }

        public class datatableModel
        {
            public int StoreID { get; set; }
            public string upc { get; set; }
            public decimal Qty { get; set; }
            public string sku { get; set; }
            public string pack { get; set; }
            public string uom { get; set; }
            public string pcat { get; set; }
            public string pcat1 { get; set; }
            public string pcat2 { get; set; }
            public string country { get; set; }
            public string region { get; set; }
            public string StoreProductName { get; set; }
            public string StoreDescription { get; set; }
            public decimal Price { get; set; }
            public decimal sprice { get; set; }
            public string Start { get; set; }
            public string End { get; set; }
            public decimal Tax { get; set; }
            public string altupc1 { get; set; }
            public string altupc2 { get; set; }
            public string altupc3 { get; set; }
            public string altupc4 { get; set; }
            public string altupc5 { get; set; }
        }
        public class ProductsModel
        {
            public int StoreID { get; set; }
            public string upc { get; set; }
            public Int64 Qty { get; set; }
            public string sku { get; set; }
            public string pack { get; set; }
            public string uom { get; set; }
            public string StoreProductName { get; set; }
            public string StoreDescription { get; set; }
            public decimal Price { get; set; }
            public decimal sprice { get; set; }
            public string Start { get; set; }
            public string End { get; set; }
            public decimal Tax { get; set; }
            public string altupc1 { get; set; }
            public string altupc2 { get; set; }
            public string altupc3 { get; set; }
            public string altupc4 { get; set; }
            public string altupc5 { get; set; }
            public decimal Deposit { get; set; }
        }
        public class ListtoDataTableConverter
        {
            public DataTable ToDataTable<T>(List<T> items, int StoreId)
            {
                DataTable dt = new DataTable(typeof(T).Name);

                PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo prop in Props)
                {
                    dt.Columns.Add(prop.Name);
                }

                foreach (T item in items)
                {
                    var values = new object[Props.Length];

                    for (int i = 0; i < Props.Length; i++)
                    {
                        //inserting property values to datatable rows
                        values[i] = Props[i].GetValue(item, null);
                    }
                    dt.Rows.Add(values);
                }
                return dt;
            }
        }

        public class Datatabletocsv
        {
            string BaseUrl = ConfigurationManager.AppSettings.Get("BaseDirectory");
            string Response = ConfigurationManager.AppSettings.Get("Regular_response");
            string add = ConfigurationManager.AppSettings.Get("add");
            string DeveloperId = ConfigurationManager.AppSettings["DeveloperId"];
            string BeerDeposit = ConfigurationManager.AppSettings.Get("BeerDeposit");
            string Sale_Price = ConfigurationManager.AppSettings.Get("Sale_Price");
            string upcfilter = ConfigurationManager.AppSettings["upcfilter"];
            string PostiveQty = ConfigurationManager.AppSettings["PostiveQty"];
            string PostiveQtyforIrregularResponce = ConfigurationManager.AppSettings["PostiveQtyforIrregularResponce"];
            public void Datatablecsv(int storeid, decimal tax, List<datatableModel> dtlist, bool IsMarkUpPrice, int MarkUpValue)
            {
                if (Response.Contains(storeid.ToString()))
                {


                    try
                    {
                        ListtoDataTableConverter cvr = new ListtoDataTableConverter();
                        DataTable dt = cvr.ToDataTable(dtlist, storeid);
                        List<ProductsModel> prodlist = new List<ProductsModel>();
                        List<FullNameProductModel> full = new List<FullNameProductModel>();
                        foreach (DataRow dr in dt.Rows)
                        {
                            ProductsModel pmsk = new ProductsModel();
                            FullNameProductModel fname = new FullNameProductModel();

                            pmsk.StoreID = storeid;

                            pmsk.sku = dr["sku"].ToString();
                            fname.sku = dr["sku"].ToString();
                            pmsk.upc = dr["upc"].ToString();
                            fname.upc = dr["upc"].ToString();

                            pmsk.StoreProductName = dr.Field<string>("StoreProductName").Trim();
                            pmsk.StoreDescription = dr.Field<string>("StoreProductName").Trim();
                            fname.pdesc = dr.Field<string>("StoreProductName").Trim();
                            fname.pname = dr.Field<string>("StoreProductName").Trim();
                            if (IsMarkUpPrice) // Added based on ticket 15542 12%MarkUp Store:11446
                            {
                                decimal price = Convert.ToDecimal(dr["Price"]);
                                decimal markup = price * MarkUpValue / 100 + price;
                                pmsk.Price = (markup);
                                pmsk.Price = Decimal.Round(pmsk.Price, 2);
                                fname.Price = Decimal.Round(pmsk.Price, 2);
                            }
                            else
                            {
                                pmsk.Price = Convert.ToDecimal(dr["Price"]);
                                fname.Price = Convert.ToDecimal(dr["Price"]);
                            }
                            if (pmsk.Price <= 0 || fname.Price <= 0)
                            {
                                continue;
                            }
                            pmsk.sprice = System.Convert.ToDecimal(dr["sprice"].ToString());
                            if (Sale_Price.Contains(storeid.ToString()) && pmsk.sprice > 0)
                            {
                                pmsk.Start = DateTime.Today.ToString("MM/dd/yyyy");
                                pmsk.End = "12/31/2999";
                            }
                            string pak = dr.Field<string>("pack");
                            pmsk.pack = Regex.Replace(pak, @"[^1-9]", "").Trim();
                            if (string.IsNullOrEmpty(pmsk.pack))
                                pmsk.pack = "1";
                            pmsk.Tax = Convert.ToDecimal(dr["Tax"]);
                            fname.pcat = dr.Field<string>("pcat").ToUpper();
                            if (fname.pcat == "HOSMER" || fname.pcat == "KEG" || fname.pcat == "RETURNS: BOTTLE/CAN" || fname.pcat == "DEPOSIT: BOTTLE/CAN"
                                || fname.pcat == "ENVIRONMENT FEE" || fname.pcat == "GIFT SALES" || fname.pcat == "DEPOSIT")
                            {
                                continue;
                            }
                            if (storeid == 11741 && fname.pcat.ToUpper() == "ALCOHOL ACCESSORIES" || fname.pcat.ToUpper() == "NON ALCOHOL")
                            {
                                pmsk.Qty = 99;
                            }
                            else
                            {
                                pmsk.Qty = Convert.ToInt32(dr["Qty"]) >= 0 ? Convert.ToInt32(dr["Qty"]) : 0;
                            }
                            fname.pcat1 = "";
                            fname.pcat2 = "";
                            fname.pack = pmsk.pack;
                            pmsk.uom = dr.Field<string>("uom");
                            if (BeerDeposit.Contains(storeid.ToString()) && pmsk.uom == "50ML")
                            {
                                continue;
                            }
                            fname.uom = pmsk.uom;
                            fname.region = "";
                            fname.country = "";

                            if (BeerDeposit.Contains(storeid.ToString())) // Added for BeerDeposits values for ticket for store 11445
                            {
                                if (fname.pcat == "BEER" || fname.pcat == "SODA" || fname.pcat == "JUICE" || fname.pcat == "CBD")
                                {
                                    double dbl = 0.05;
                                    decimal dec = (decimal)dbl;
                                    pmsk.Deposit = Convert.ToDecimal(pmsk.pack) * dec;
                                }
                            }
                            if (PostiveQty.Contains(storeid.ToString()))
                            {
                                if (pmsk.Qty > 0)
                                {
                                    prodlist.Add(pmsk);
                                    full.Add(fname);
                                }
                            }
                            else 
                            {
                                prodlist.Add(pmsk);
                                full.Add(fname);
                            }
                        }
                        Console.WriteLine("Generating PTECHPOS " + storeid + " Product CSV Files.....");
                        string filename = GenerateCSV.GenerateCSVFile(prodlist, "PRODUCT", storeid, BaseUrl);
                        Console.WriteLine("Product File Generated For PTECHPOS " + storeid);
                        Console.WriteLine();
                        Console.WriteLine("Generating PTECHPOS " + storeid + " Fullname CSV Files.....");
                        filename = GenerateCSV.GenerateCSVFile(full, "FULLNAME", storeid, BaseUrl);
                        Console.WriteLine("Fullname File Generated For PTECHPOS " + storeid);
                    }

                    catch (Exception ex)
                    {
                        Console.WriteLine("PTECH" + ex.Message);
                        (new clsEmail()).sendEmail(DeveloperId, "", "", "Error in " + storeid + " Ptech@" + DateTime.UtcNow + " GMT", "StatusCode:ERROR Response2" + "<br/>" + ex.Message + "<br/>" + ex.StackTrace);
                    }
                }
                else
                {

                    try
                    {
                        ListtoDataTableConverter cvr = new ListtoDataTableConverter();

                        DataTable dt = cvr.ToDataTable(dtlist, storeid);
                        var dtr = from s in dt.AsEnumerable() select s;
                        List<ProductsModel> prodlist = new List<ProductsModel>();
                        List<FullNameProductModel> full = new List<FullNameProductModel>();

                        dynamic upcs;
                        //dynamic taxs;
                        int barlenth = 0;

                        foreach (DataRow dr in dt.Rows)
                        {
                            ProductsModel pmsk = new ProductsModel();
                            FullNameProductModel fname = new FullNameProductModel();
                            dt.DefaultView.Sort = "sku";
                            upcs = dt.DefaultView.FindRows(dr["sku"]).ToArray();
                            barlenth = ((Array)upcs).Length;
                            pmsk.StoreID = storeid;

                            if (barlenth > 0)
                            {
                                for (int i = 0; i <= barlenth - 1; i++)
                                {
                                    if (i == 0)
                                    {
                                        if (!string.IsNullOrEmpty(dr["upc"].ToString()))
                                        {
                                            var upc = "#" + upcs[i]["upc"].ToString().ToLower();
                                            string numberUpc = Regex.Replace(upc, "[^0-9.]", "");
                                            if (!upcfilter.Contains(storeid.ToString()))
                                            {
                                                if (numberUpc.Count() >= 6)
                                                {
                                                    if (!string.IsNullOrEmpty(numberUpc))
                                                    {
                                                        pmsk.upc = "#" + numberUpc.Trim().ToLower();
                                                        fname.upc = "#" + numberUpc.Trim().ToLower();
                                                    }
                                                    else
                                                    {
                                                        continue;
                                                    }
                                                }
                                                else
                                                {
                                                    continue;
                                                }
                                            }
                                            else
                                            {
                                                if (numberUpc.Count() >= 10)
                                                {
                                                    if (!string.IsNullOrEmpty(numberUpc))
                                                    {
                                                        pmsk.upc = "#" + numberUpc.Trim().ToLower();
                                                        fname.upc = "#" + numberUpc.Trim().ToLower();
                                                    }
                                                    else
                                                    {
                                                        continue;
                                                    }
                                                }
                                                else if (numberUpc.Count() < 6)
                                                {
                                                    pmsk.upc = "#9911716" + numberUpc.Trim().ToLower();
                                                    fname.upc = "#9911716" + numberUpc.Trim().ToLower();
                                                }
                                                else if (numberUpc.Count() >= 6 && numberUpc.Count() < 10)
                                                {
                                                    pmsk.upc = "#11716" + numberUpc.Trim().ToLower();
                                                    fname.upc = "#11716" + numberUpc.Trim().ToLower();
                                                }
                                            }

                                        }
                                        else
                                        {
                                            continue;
                                        }
                                    }
                                    if (i == 1)
                                    {
                                        pmsk.altupc1 = "#" + upcs[i]["upc"];
                                    }
                                    if (i == 2)
                                    {
                                        pmsk.altupc2 = "#" + upcs[i]["upc"];
                                    }
                                    if (i == 3)
                                    {
                                        pmsk.altupc3 = "#" + upcs[i]["upc"];
                                    }
                                    if (i == 4)
                                    {
                                        pmsk.altupc4 = "#" + upcs[i]["upc"];
                                    }
                                    if (i == 5)
                                    {
                                        pmsk.altupc5 = "#" + upcs[i]["upc"];
                                    }
                                }
                            }
                            if (!string.IsNullOrEmpty(dr["sku"].ToString()))
                            {
                                pmsk.sku = "#" + dr["sku"].ToString();
                                fname.sku = "#" + dr["sku"].ToString();
                            }
                            else
                            { continue; }
                            pmsk.Qty = Convert.ToInt32(dr["Qty"]);
                            pmsk.StoreProductName = dr.Field<string>("StoreProductName").Trim();
                            pmsk.StoreDescription = dr.Field<string>("StoreProductName").Trim();
                            fname.pdesc = dr.Field<string>("StoreProductName").Trim();
                            fname.pname = dr.Field<string>("StoreProductName").Trim();

                            pmsk.Price = System.Convert.ToDecimal(dr["Price"].ToString());
                            fname.Price = System.Convert.ToDecimal(dr["Price"].ToString());
                            if (pmsk.Price <= 0 || fname.Price <= 0)
                            {
                                continue;
                            }
                            pmsk.sprice = System.Convert.ToDecimal(dr["sprice"].ToString());
                            pmsk.pack = 1.ToString();
                            pmsk.Tax = Convert.ToDecimal(dr["Tax"]);
                            if (pmsk.sprice > 0)
                            {
                                pmsk.Start = DateTime.Now.ToString("MM/dd/yyyy");
                                pmsk.End = DateTime.Now.AddDays(1).ToString("MM/dd/yyyy");
                            }
                            else
                            {
                                pmsk.Start = "";
                                pmsk.End = "";
                            }
                            fname.pcat = dr.Field<string>("pcat");
                            fname.pcat1 = dr.Field<string>("pcat1");
                            fname.pcat2 = "";
                            fname.pack = 1.ToString();
                            pmsk.uom = dr.Field<string>("uom");
                            fname.uom = dr.Field<string>("uom");
                            fname.region = "";
                            fname.country = "";

                            if (add.Contains(storeid.ToString()))
                            {
                                if (pmsk.Price > 0 && !string.IsNullOrEmpty(pmsk.upc) && pmsk.sku != "#50112" && pmsk.sku != "#63479" && pmsk.sku != "#63478" && pmsk.sku != "#50087" && pmsk.sku != "#51602" && fname.pcat != "CIGARETTES" && fname.pcat != "CIGARS" && fname.pcat != "TOBACCO" && fname.pcat != "CIG" && fname.pcat != "CIGARILLOS" && fname.pcat != "CIGAR" && fname.pcat != "CIGERATT" && fname.pcat != "CIGERATTE" && fname.pcat != "Tobacco" && fname.pcat != "CIGARETTE" && fname.pcat != "CIGARS & MISC TOBACCO" && fname.pcat != "SMOKELESS TOBACCO" && fname.pcat != "LIGHTERS & SMOKING ACCESSORIES" && fname.pcat != "HUMIDIFIER CIGAR" && fname.pcat != "PP LIGHTERS & SMOKING" && fname.pcat != "NONE" && fname.pcat != "NOVELTY" && fname.pcat != "VAPOR" && fname.pcat != "" && fname.pcat != "TEST")
                                {
                                    prodlist.Add(pmsk);
                                    prodlist = prodlist.GroupBy(x => x.sku).Select(y => y.FirstOrDefault()).ToList();
                                    prodlist = prodlist.GroupBy(x => x.upc).Select(y => y.FirstOrDefault()).ToList();
                                    full.Add(fname);
                                    full = full.GroupBy(x => x.sku).Select(y => y.FirstOrDefault()).ToList();
                                    full = full.GroupBy(x => x.upc).Select(y => y.FirstOrDefault()).ToList();
                                }

                            }
                            else if (PostiveQtyforIrregularResponce.Contains(storeid.ToString()))
                            {
                                if (pmsk.Price > 0 && pmsk.Qty > 0 && !string.IsNullOrEmpty(pmsk.upc) && fname.pcat != "CIGARETTES" && fname.pcat != "CIGARS" && fname.pcat != "TOBACCO" && fname.pcat != "CIG" && fname.pcat != "CIGARILLOS" && fname.pcat != "CIGAR" && fname.pcat != "CIGERATT" && fname.pcat != "CIGERATTE" && fname.pcat != "Tobacco" && fname.pcat != "CIGARETTE" && fname.pcat != "CIGARS & MISC TOBACCO" && fname.pcat != "SMOKELESS TOBACCO" && fname.pcat != "LIGHTERS & SMOKING ACCESSORIES" && fname.pcat != "HUMIDIFIER CIGAR" && fname.pcat != "PP LIGHTERS & SMOKING" && fname.pcat != "TOBACCO-CIGARETTES" && fname.pcat != "TOBACCO-CIGARS")
                                {
                                    prodlist.Add(pmsk);
                                    prodlist = prodlist.GroupBy(x => x.sku).Select(y => y.FirstOrDefault()).ToList();
                                    prodlist = prodlist.GroupBy(x => x.upc).Select(y => y.FirstOrDefault()).ToList();
                                    full.Add(fname);
                                    full = full.GroupBy(x => x.sku).Select(y => y.FirstOrDefault()).ToList();
                                    full = full.GroupBy(x => x.upc).Select(y => y.FirstOrDefault()).ToList();
                                }
                            }
                            else
                            {
                                if (pmsk.Price > 0 && !string.IsNullOrEmpty(pmsk.upc) && fname.pcat != "CIGARETTES" && fname.pcat != "CIGARS" && fname.pcat != "TOBACCO" && fname.pcat != "CIG" && fname.pcat != "CIGARILLOS" && fname.pcat != "CIGAR" && fname.pcat != "CIGERATT" && fname.pcat != "CIGERATTE" && fname.pcat != "Tobacco" && fname.pcat != "CIGARETTE" && fname.pcat != "CIGARS & MISC TOBACCO" && fname.pcat != "SMOKELESS TOBACCO" && fname.pcat != "LIGHTERS & SMOKING ACCESSORIES" && fname.pcat != "HUMIDIFIER CIGAR" && fname.pcat != "PP LIGHTERS & SMOKING" && fname.pcat != "TOBACCO-CIGARETTES" && fname.pcat != "TOBACCO-CIGARS")
                                {
                                    prodlist.Add(pmsk);
                                    prodlist = prodlist.GroupBy(x => x.sku).Select(y => y.FirstOrDefault()).ToList();
                                    prodlist = prodlist.GroupBy(x => x.upc).Select(y => y.FirstOrDefault()).ToList();
                                    full.Add(fname);
                                    full = full.GroupBy(x => x.sku).Select(y => y.FirstOrDefault()).ToList();
                                    full = full.GroupBy(x => x.upc).Select(y => y.FirstOrDefault()).ToList();
                                }
                            }
                        }
                        Console.WriteLine("Generating PTECHPOS " + storeid + " Product CSV Files.....");
                              string filename = GenerateCSV.GenerateCSVFile(prodlist, "PRODUCT", storeid, BaseUrl);
                        Console.WriteLine("Product File Generated For PTECHPOS " + storeid);
                        Console.WriteLine();
                        Console.WriteLine("Generating PTECHPOS " + storeid + " Fullname CSV Files.....");
                        filename = GenerateCSV.GenerateCSVFile(full, "FULLNAME", storeid, BaseUrl);
                        Console.WriteLine("Fullname File Generated For PTECHPOS " + storeid);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("PTECH" + ex.Message);
                        (new clsEmail()).sendEmail(DeveloperId, "", "", "Error in " + storeid + " Ptech@" + DateTime.UtcNow + " GMT", "StatusCode:ERROR GEnerating Files" + "<br/>" + ex.Message + "<br/>" + ex.StackTrace);
                    }
                }
            }
        }

    }

}

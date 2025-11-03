using Ptech.Models;
using PTech;
using PTech.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static PTech.clsPTECH;

namespace Ptech
{
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
        string IncludePack = ConfigurationManager.AppSettings["IncludePack"];
        public void Datatablecsv(int storeid, decimal tax, List<datatableModel> dtlist, bool IsMarkUpPrice, int MarkUpValue)
        {
            if (Response.Contains(storeid.ToString()))
            {
                try
                {
                    //ListtoDataTableConverter cvr = new ListtoDataTableConverter();
                    //DataTable dt = cvr.ToDataTable(dtlist, storeid);
                    List<ProductsModel> prodlist = new List<ProductsModel>();
                    List<FullNameProductModel> full = new List<FullNameProductModel>();
                    foreach(datatableModel dr in dtlist)
                    {
                        ProductsModel pmsk = new ProductsModel();
                        FullNameProductModel fname = new FullNameProductModel();

                        pmsk.StoreID = storeid;
                        pmsk.upc = dr.upc;
                        fname.upc = pmsk.upc;
                        pmsk.sku = dr.sku;
                        fname.sku = pmsk.sku;

                        pmsk.StoreProductName = dr.StoreProductName;
                        pmsk.StoreDescription = pmsk.StoreProductName;
                        fname.pname = pmsk.StoreProductName;
                        fname.pdesc = pmsk.StoreDescription;

                        if (IsMarkUpPrice)// Added based on ticket 15542 12% MarkUp Store:11446
                        {
                            decimal price = Convert.ToDecimal(dr.Price);
                            decimal markup = price * MarkUpValue / 100 + price;
                            pmsk.Price = (markup);
                            pmsk.Price = Decimal.Round(pmsk.Price, 2);
                            fname.Price = Decimal.Round(pmsk.Price, 2);
                        }
                        else
                        {
                            pmsk.Price = Convert.ToDecimal(dr.Price);
                            fname.Price = Convert.ToDecimal(dr.Price);
                        }
                        if (pmsk.Price <= 0 || fname.Price <= 0)
                        {
                            continue;
                        }
                        pmsk.sprice = System.Convert.ToDecimal(dr.sprice.ToString());
                        if (Sale_Price.Contains(storeid.ToString()) && pmsk.sprice > 0)
                        {
                            pmsk.Start = DateTime.Today.ToString("MM/dd/yyyy");
                            pmsk.End = "12/31/2999";
                        }

                        string pak = dr.pack;
                        pmsk.pack = Regex.Replace(pak, @"[^1-9]", "").Trim();
                        if (string.IsNullOrEmpty(pmsk.pack))
                            pmsk.pack = "1";
                        pmsk.Tax = dr.Tax;
                        fname.pcat = dr.pcat.ToUpper();
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
                            pmsk.Qty = Convert.ToInt32(dr.Qty) >= 0 ? Convert.ToInt32(dr.Qty) : 0;
                        }
                        fname.pcat1 = "";
                        fname.pcat2 = "";
                        fname.pack = pmsk.pack;
                        pmsk.uom = dr.uom;
                        if (BeerDeposit.Contains(storeid.ToString()) && pmsk.uom == "50ML")
                        {
                            continue;
                        }

                        if (storeid == 11873)
                        {
                            if (pmsk.uom == "50 ml")
                            {
                                pmsk.EnvironmentalFee = 1;
                            }
                            else
                            {
                                pmsk.EnvironmentalFee = 0;
                            }
                        }
                        else
                        {
                            pmsk.EnvironmentalFee = 0;
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

                        if (storeid == 11873)
                        {
                            if (fname.pcat == "BEER" || fname.pcat == "SODA" || fname.pcat == "MIXER")
                            {
                                double dbl = 0.10;
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
                    //ListtoDataTableConverter cvr = new ListtoDataTableConverter();

                    //DataTable dt = cvr.ToDataTable(dtlist, storeid);
                    //var dtr = from s in dt.AsEnumerable() select s;
                    List<ProductsModel> prodlist = new List<ProductsModel>();
                    List<FullNameProductModel> full = new List<FullNameProductModel>();
                    var groupedBySku = dtlist.GroupBy(x => x.sku).ToList();

                    foreach (var group in groupedBySku)
                    {
                        var items = group.ToList();
                        var first = items.FirstOrDefault();
                        if (first == null) 
                            continue;

                        var pmsk = new ProductsModel();
                        var fname = new FullNameProductModel();

                        pmsk.StoreID = storeid;
                        if (!string.IsNullOrEmpty(first.upc))
                        {
                            string upc = "#" + first.upc.ToLower();
                            string numberUpc = Regex.Replace(upc, "[^0-9.]", "");

                            if (!upcfilter.Contains(storeid.ToString()))
                            {
                                if (numberUpc.Length >= 6)
                                {
                                    pmsk.upc = "#" + numberUpc;
                                    fname.upc = pmsk.upc;
                                }
                                else 
                                    continue;
                            }
                            else
                            {
                                if (numberUpc.Length >= 10)
                                {
                                    pmsk.upc = "#" + numberUpc;
                                    fname.upc = "#" + numberUpc;
                                }
                                else if (numberUpc.Length < 6)
                                {
                                    pmsk.upc = "#9911716" + numberUpc;
                                    fname.upc = pmsk.upc;
                                }
                                else if (numberUpc.Length >= 6 && numberUpc.Length < 10)
                                {
                                    pmsk.upc = "#11716" + numberUpc;
                                    fname.upc = pmsk.upc;
                                }
                            }
                        }
                        else 
                            continue;

                        if (items.Count > 1) pmsk.altupc1 = items[1].upc;
                        if (items.Count > 2) pmsk.altupc2 = items[2].upc;
                        if (items.Count > 3) pmsk.altupc3 = items[3].upc;
                        if (items.Count > 4) pmsk.altupc4 = items[4].upc;
                        if (items.Count > 5) pmsk.altupc5 = items[5].upc;

                        pmsk.sku = first.sku?.ToString();
                        if (string.IsNullOrEmpty(pmsk.sku)) 
                            continue;
                        fname.sku = pmsk.sku;

                        pmsk.Qty = (int)first.Qty;

                        pmsk.StoreProductName = first.StoreProductName?.Trim();
                        pmsk.StoreDescription = pmsk.StoreProductName;
                        fname.pname = pmsk.StoreProductName;
                        fname.pdesc = pmsk.StoreProductName;

                        pmsk.Price = Convert.ToDecimal(first.Price);
                        fname.Price = pmsk.Price;
                        if (pmsk.Price <= 0) 
                            continue;
                        pmsk.sprice = Convert.ToDecimal(first.sprice);

                        if (IncludePack.Contains(storeid.ToString()))
                        {
                            pmsk.pack = first.pack?.ToString() ?? "1";
                            fname.pack = pmsk.pack;
                        }
                        else if (string.IsNullOrEmpty(first.pack?.ToString()))
                        {
                            pmsk.pack = "1";
                            fname.pack = "1";
                        }
                        else
                        {
                            pmsk.pack = first.pack?.ToString();
                            fname.pack = pmsk.pack;
                        }

                        pmsk.Tax = Convert.ToDecimal(first.Tax);

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

                        fname.pcat = first.pcat;
                        fname.pcat1 = first.pcat1;
                        fname.pcat2 = "";
                        pmsk.uom = first.uom;
                        fname.uom = pmsk.uom;
                        fname.region = "";
                        fname.country = "";

                        string[] excludedCats = {
                                                    "CIGARETTES","CIGARS","TOBACCO","CIG","CIGARILLOS","CIGAR",
                                                    "CIGERATT","CIGERATTE","Tobacco","CIGARETTE","CIGARS & MISC TOBACCO",
                                                    "SMOKELESS TOBACCO","LIGHTERS & SMOKING ACCESSORIES","HUMIDIFIER CIGAR",
                                                    "PP LIGHTERS & SMOKING","NONE","NOVELTY","VAPOR","TEST","TOBACCO-CIGARETTES","TOBACCO-CIGARS"
                                                };
                        if (excludedCats.Contains(fname.pcat)) 
                            continue;

                        bool shouldAdd = false;
                        if (add.Contains(storeid.ToString()))
                        {
                            shouldAdd = pmsk.Price > 0 && !string.IsNullOrEmpty(pmsk.upc)
                                        && pmsk.sku != "#50112" && pmsk.sku != "#63479" && pmsk.sku != "#63478"
                                        && pmsk.sku != "#50087" && pmsk.sku != "#51602";
                        }
                        else if (PostiveQtyforIrregularResponce.Contains(storeid.ToString()))
                        {
                            shouldAdd = pmsk.Price > 0 && pmsk.Qty > 0 && !string.IsNullOrEmpty(pmsk.upc);
                        }
                        else
                        {
                            shouldAdd = pmsk.Price > 0 && !string.IsNullOrEmpty(pmsk.upc);
                        }
                        if (shouldAdd)
                        {
                            prodlist.Add(pmsk);
                            full.Add(fname);
                        }
                    }

                    prodlist = prodlist.GroupBy(x => x.sku).Select(y => y.First()).ToList();
                    prodlist = prodlist.GroupBy(x => x.upc).Select(y => y.First()).ToList();

                    full = full.GroupBy(x => x.sku).Select(y => y.First()).ToList();
                    full = full.GroupBy(x => x.upc).Select(y => y.First()).ToList();

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
}

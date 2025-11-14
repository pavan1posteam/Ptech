

using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
//using Ptech.Models;
using PTech.Models;
using System.Text.RegularExpressions;

namespace Ptech
{
    

    class Shopifypos
    {
        public class Shopify
        {
            string folderPath = ConfigurationManager.AppSettings.Get("BaseDirectory");


            public Shopify(int storeId, decimal tax, string BaseUrl, string accesstoken)
            {
                shopifyCsvConverter(storeId, tax, BaseUrl, accesstoken);
            }
            public void shopifyCsvConverter(int storeId, decimal tax, string BaseUrl, string accesstoken)
            {
                var productList = Products(storeId, tax, BaseUrl, accesstoken);

                //   string folderPath = ConfigurationManager.AppSettings.Get("BaseDirectory"); 

                List<ProductModel> pf = new List<ProductModel>();
                List<FullNameProductModel> full = new List<FullNameProductModel>();
                foreach (var item in productList)
                {
                    ProductModel pdf = new ProductModel();
                    FullNameProductModel fname = new FullNameProductModel();
                    pdf.StoreID = storeId;

                    // for accessing variants
                    var variant = item.variants?.FirstOrDefault();
                    if (variant != null)
                    {
                          pdf.upc = variant.barcode;
                        if (string.IsNullOrEmpty(pdf.upc))
                        {
                            pdf.upc = variant.product_id.ToString();
                        }
                        pdf.sku = variant.product_id.ToString() ;    // using product id as sku
                        pdf.Qty = variant.inventory_quantity;
                        pdf.Price = Convert.ToDecimal(variant.price);

                    }
                    else
                    {
                        pdf.upc = item.id.ToString() ;    // if variants is null then fall back to top level id
                        pdf.sku = item.id.ToString() ;   
                        pdf.Qty = 0;
                        pdf.Price = 0;
                    }

                    pdf.pack = Convert.ToInt32(getpack(item.title.ToString()));
                    pdf.uom = getVolume(item.title.ToString());
                    pdf.StoreProductName = item.title;
                    pdf.StoreDescription = pdf.StoreProductName;
                    pdf.sprice = 0;
                    pdf.Start = "";
                    pdf.End = "";
                    pdf.Tax = tax;
                    pdf.altupc1 = "";
                    pdf.altupc2 = "";
                    pdf.altupc3 = "";
                    pdf.altupc4 = "";

                    fname.pname = pdf.StoreProductName;
                    fname.pdesc = pdf.StoreDescription;
                    fname.upc = pdf.upc;
                    fname.sku = pdf.sku;
                    fname.Price = pdf.Price;
                    fname.uom = pdf.uom;
                    fname.pack = pdf.pack.ToString();
                    fname.pcat = item.product_type;

                    pf.Add(pdf);
                    full.Add(fname);
                }
                Console.WriteLine("Generating SHOPIFYPOS " + storeId + " Product CSV Files.....");
                string filename = GenerateCSV.GenerateCSVFile(pf, "PRODUCT", storeId, folderPath);
                Console.WriteLine("Product File Generated For SHOPIFYPOS " + storeId);
                Console.WriteLine("Generating SHOPIFYPOS " + storeId + " Fullname CSV Files.....");
                filename = GenerateCSV.GenerateCSVFile(full, "FULLNAME", storeId, folderPath);
                Console.WriteLine("Fullname File Generated For SHOPIFYPOS " + storeId);
            }

            public List<Product> Products(int StoreId, decimal tax, string BaseUrl, string accesstoken)
            {
                List<Product> productList = new List<Product>();

                try
                {
                    BaseUrl += "/admin/api/2025-01/products.json";  //appending the end point to base url (mostly store name)
                    var client = new RestClient(BaseUrl);
                    client.Timeout = -1;
                    var request = new RestRequest(Method.GET);
                    request.AddHeader("X-Shopify-Access-Token", accesstoken);

                    // request.AddHeader("Password", Password); 

                    request.AddHeader("Content-Type", "application/json");

                    //  request.AddParameter("application/json", ParameterType.RequestBody);  

                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    IRestResponse response = client.Execute(request);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var content = response.Content;
                        var jsons = JsonConvert.DeserializeObject<Root>(content);
                        var model = jsons.products;
                        productList = model;
                    }
                    //File.AppendAllText("11714.json", response.Content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "shopify");
                }
                return productList;
            }


            public int getpack(string prodName)
            {
                prodName = prodName.ToUpper();
                var regexMatch = Regex.Match(prodName, @"(?<Result>\d+)PK");
                var prodPack = regexMatch.Groups["Result"].Value;
                if (prodPack.Length > 0)
                {
                    int outVal = 0;
                    int.TryParse(prodPack.Replace("$", ""), out outVal);
                    return outVal;
                }
                return 1;
            }

            public string getVolume(string prodName)
            {
                prodName = prodName.ToUpper();
                var regexMatch = Regex.Match(prodName, @"(?<Result>\d+)ML| (?<Result>\d+)LTR| (?<Result>\d+)OZ | (?<Result>\d+)L|(?<Result>\d+)OZ");
                var prodPack = regexMatch.Groups["Result"].Value;
                if (prodPack.Length > 0)
                {
                    return regexMatch.ToString();
                }
                return "";
            }



            //  shopify inner class
        }

        public class Variant
        {
            public int inventory_quantity { get; set; }   // Quantity
            public string price { get; set; }
            public string barcode { get; set; }    //UPC 

            public long product_id { get; set; }   // alternative for upc and sku 

            //    public long id { get; set; }    // not required  

            //   public string title { get; set; }   

            //    public int position { get; set; }
            //   public string inventory_policy { get; set; }
            //  public string compare_at_price { get; set; }
            //  public string option1 { get; set; }
            //  public string option2 { get; set; }
            //  public string option3 { get; set; }
            //  public DateTime created_at { get; set; }
            //  public DateTime updated_at { get; set; }
            //  public bool taxable { get; set; }


            //   public string fulfillment_service { get; set; }
            //   public int grams { get; set; }
            //   public string inventory_management { get; set; }
            //   public bool requires_shipping { get; set; }
            //   public string sku { get; set; }
            //   public double weight { get; set; }
            // public string weight_unit { get; set; }
            //   public long inventory_item_id { get; set; }

            //   public int old_inventory_quantity { get; set; }
            //   public string admin_graphql_api_id { get; set; }
            //   public object image_id { get; set; }
        }

        /*   public class Option
           {
               //public long id { get; set; }
               //public long product_id { get; set; }
               //public string name { get; set; }
               //public int position { get; set; }
               public List<string> values { get; set; }
           }
               */
        /*   public class Image
           {

               //public long id { get; set; }
               //public string alt { get; set; }
               //public int position { get; set; }
               //public long product_id { get; set; }
           //    public DateTime created_at { get; set; }
           //    public DateTime updated_at { get; set; }
          //     public string admin_graphql_api_id { get; set; }
         //      public int width { get; set; }
          //     public int height { get; set; }
           //    public string src { get; set; }
               public List<object> variant_ids { get; set; }
           }   */

        public class Product
        {

            public string title { get; set; }
            public List<Variant> variants { get; set; }   // imp for reading 
            public string product_type { get; set; }    // category wine,vodka   

            public long id { get; set; }   // this is also product_id (if variants null use this)

            //    public string body_html { get; set; }
            //   public string vendor { get; set; }
            //  public DateTime created_at { get; set; }
            //   public string handle { get; set; }
            //   public DateTime updated_at { get; set; }
            //  public DateTime published_at { get; set; }
            //   public string template_suffix { get; set; }

            //  public string published_scope { get; set; } 

            //   public string tags { get; set; }
            //  public string status { get; set; }
            //   public string admin_graphql_api_id { get; set; }
            /*            public List<Option> options { get; set; }
                        public List<Image> images { get; set; }
                        public Image image { get; set; }*/
        }

        // this class for reading all the data from response as list 
        public class Root
        {
            public List<Product> products { get; set; }
        }



    }




}
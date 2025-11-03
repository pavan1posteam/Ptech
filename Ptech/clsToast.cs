using Newtonsoft.Json;
using PTech.Models;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Ptech
{
    internal class clsToast
    {
        string folderPath = ConfigurationManager.AppSettings.Get("BaseDirectory");
        string PostiveQty = ConfigurationManager.AppSettings["PostiveQty"];
        string StaticQty = ConfigurationManager.AppSettings["Qty"];
        public readonly int StoreId;
        public readonly decimal Tax;
        public readonly string BaseUrl;
        public readonly string ClientId;
        public readonly string ClientSecret;
        public readonly string Type;
        public readonly string GUID;
        public readonly bool Loyalty;
        public string AccessToken;
        public clsToast(int _storeid, decimal _tax, string _baseurl, string _clientid, string _clientsecret, string _type, string _guid, bool _loyalty)
        {
            StoreId = _storeid;
            Tax = _tax;
            BaseUrl = _baseurl;
            ClientId = _clientid;
            ClientSecret = _clientsecret;
            Type = _type;
            GUID = _guid;
            Loyalty = _loyalty;
        }
        public string run()
        {
            Console.WriteLine("Store: " + StoreId);
            authenticate();
            if(!string.IsNullOrEmpty(AccessToken))
            {
                List<RootQty> QTYData = getQuantity();
                List<InventoryItem> InventoryList = GetInventory();
                Convert2csv(QTYData, InventoryList);
                return "Success";
            }
            return "Fail AccessToken Is NULL";
        }
        public string authenticate()
        {
            try
            {
                string Url = BaseUrl + "/authentication/v1/authentication/login";
                var client = new RestClient(Url);
                var request = new RestRequest(Method.POST);

                var payload = new
                {
                    clientId = ClientId,
                    clientSecret = ClientSecret,
                    userAccessType = Type
                };

                string json = JsonConvert.SerializeObject(payload);

                request.AddParameter("application/json", json, ParameterType.RequestBody);

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                IRestResponse response = client.Execute(request);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Root val = JsonConvert.DeserializeObject<Root>(response.Content);
                    AccessToken = val.token.accessToken;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Authentication Error StoreId: " + StoreId);
                Console.WriteLine("Error: " + ex.ToString());
            }

            return AccessToken;
        }
        public List<RootQty> getQuantity()
        {
            try
            {
                string Url = BaseUrl + "/stock/v1/inventory";
                var client = new RestClient(Url);
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization","Bearer " + AccessToken);
                request.AddHeader("Toast-Restaurant-External-ID", GUID);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                IRestResponse response = client.Execute(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    List<RootQty> QTYData = JsonConvert.DeserializeObject<List<RootQty>>(response.Content);
                    return QTYData;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
            }
            return new List<RootQty>();
        }
        public List<InventoryItem> InventoryList { get; set; } = new List<InventoryItem>();
        public List<InventoryItem> GetInventory()
        {
            try
            {
                string Url = BaseUrl + "/menus/v2/menus";
                var client = new RestClient(Url);
                var request = new RestRequest(Method.GET);
                request.AddHeader("Authorization", "Bearer " + AccessToken);
                request.AddHeader("Toast-Restaurant-External-ID", GUID);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                IRestResponse response = client.Execute(request);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    RootMenu data = JsonConvert.DeserializeObject<RootMenu>(response.Content);
                    //File.AppendAllText("12381.json", response.Content);
                    #region
                    //InventoryList = data.menus
                    //                    .SelectMany(menu => menu.menuGroups ?? Enumerable.Empty<MenuGroup>(), (menu, group) => new { menu, group })
                    //                    .SelectMany(x => x.group.menuItems ?? Enumerable.Empty<MenuItem>(), (x, item) => new InventoryItem
                    //                    {
                    //                        MenuName = x.menu.name ?? "",
                    //                        GroupName = item.salesCategory?.name ?? "", 
                    //                        ItemName = item.name ?? "",
                    //                        Guid = item.guid ?? "",
                    //                        Description = item.description.Replace("\r", "").Replace("\n", "") ?? "",
                    //                        Price = item.price ?? 0m,
                    //                        IsDiscountable = item.isDiscountable ?? false,
                    //                        Sku = item.sku ?? ""
                    //                    }).ToList();
                    //InventoryList = data.menus
                    //                    .SelectMany(menu => menu.menuGroups ?? Enumerable.Empty<MenuGroup>(), (menu, group) => new { RootMenuName = menu.name, group })
                    //                    .SelectMany(x => x.group.menuItems ?? Enumerable.Empty<MenuItem>(), (x, item) => new InventoryItem
                    //                    {
                    //                        MenuName = x.RootMenuName ?? "",             
                    //                        GroupName = x.group.name ?? "",                  
                    //                        ItemName = item.name ?? "",
                    //                        Guid = item.guid ?? "",
                    //                        Description = (item.description ?? "").Replace("\r", "").Replace("\n", ""),
                    //                        Price = item.price ?? 0m,
                    //                        IsDiscountable = item.isDiscountable ?? false,
                    //                        Sku = item.sku ?? ""
                    //                    }).ToList();
                    #endregion
                    InventoryList = new List<InventoryItem>();

                    foreach (var menu in data.menus ?? Enumerable.Empty<Menu>())
                    {
                        InventoryList.AddRange(FlattenMenuGroups(menu.name, null, null, menu.menuGroups));
                    }


                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
            return InventoryList;
        }
        private IEnumerable<InventoryItem> FlattenMenuGroups(string menuName,string groupName1,string groupName2,List<MenuGroup> groups)
        {
            foreach (var group in groups ?? Enumerable.Empty<MenuGroup>())
            {
                foreach (var item in group.menuItems ?? Enumerable.Empty<MenuItem>())
                {
                    yield return new InventoryItem
                    {
                        MenuName = menuName ?? "",
                        GroupName1 = groupName1 ?? "",
                        GroupName2 = groupName2 ?? "",
                        GroupName3 = group.name ?? "",
                        ItemName = item.name ?? "",
                        Guid = item.guid ?? "",
                        Description = (item.description ?? "").Replace("\r", "").Replace("\n", ""),
                        Price = item.price ?? 0m,
                        IsDiscountable = item.isDiscountable ?? false,
                        Sku = item.sku ?? ""
                    };
                }
                foreach (var deeper in FlattenMenuGroups(menuName, groupName1 ?? group.name, groupName2 ?? group.name, group.menuGroups ?? new List<MenuGroup>()))
                {
                    yield return deeper;
                }
            }
        }

        public void Convert2csv(List<RootQty> QTYData, List<InventoryItem> InventoryList)
        {
            List<ProductModel> pf = new List<ProductModel>();
            List<FullNameProductModel> full = new List<FullNameProductModel>();
            foreach (InventoryItem item in InventoryList)
            {
                ProductModel pdf = new ProductModel();
                FullNameProductModel fname = new FullNameProductModel();
                pdf.StoreID = StoreId;
                if (string.IsNullOrEmpty(item.Sku))
                    continue;
                pdf.upc = "#" + item.Sku;
                pdf.sku = "#" + item.Sku;
                pdf.StoreProductName = item.ItemName;
                if (string.IsNullOrEmpty(item.Description))
                    pdf.StoreDescription = pdf.StoreProductName;
                else
                    pdf.StoreDescription = item.Description;
                pdf.Price = item.Price;
                pdf.Tax = Tax;
                pdf.pack = getpack(pdf.StoreProductName);
                pdf.uom = getVolume(pdf.StoreProductName);
                if((bool)item.IsDiscountable)
                    pdf.Discountable = 1;
                else
                    pdf.Discountable = 0;
                if (StaticQty.Contains(StoreId.ToString()))
                    pdf.Qty = 999;
                else
                {
                    int validation = 0;
                    foreach (RootQty rootQty in QTYData)
                    {
                        if (item.Guid == rootQty.guid)
                        {
                            validation = 1;
                            if (!string.IsNullOrEmpty(rootQty.quantity))
                                pdf.Qty = Convert.ToInt32(rootQty.quantity);
                            else
                                pdf.Qty = 0;
                            break;
                        }
                    }
                    if (validation == 0)
                        continue;
                }
                fname.pname = pdf.StoreProductName;
                fname.pdesc = pdf.StoreDescription;
                fname.upc = pdf.upc;
                fname.sku = pdf.sku;
                fname.Price = pdf.Price;
                fname.uom = pdf.uom;
                fname.pack = pdf.pack.ToString();
                fname.pcat = item.MenuName;
                if (string.IsNullOrEmpty(item.GroupName1))
                    if (string.IsNullOrEmpty(item.GroupName2))
                        fname.pcat1 = item.GroupName3;
                    else
                        fname.pcat1 = item.GroupName2;
                else
                    fname.pcat1 = item.GroupName1;
                if (string.IsNullOrEmpty(item.GroupName2))
                    fname.pcat2 = item.GroupName3;
                else
                    fname.pcat2 = item.GroupName2;
                if (fname.pcat.ToUpper().Contains("THC"))
                    continue;
                if (PostiveQty.Contains(StoreId.ToString()))
                {
                    if (pdf.Qty > 0 && pdf.Price > 0)
                    {
                        pf.Add(pdf);
                        full.Add(fname);
                    }
                }
                else if(pdf.Price > 0)
                {
                    pf.Add(pdf);
                    full.Add(fname);
                }
                
            }
            Console.WriteLine("Generating TOASTPOS " + StoreId + " Product CSV Files.....");
            string filename = GenerateCSV.GenerateCSVFile(pf, "PRODUCT", StoreId, folderPath);
            Console.WriteLine("Product File Generated For TOASTPOS " + StoreId);
            Console.WriteLine("Generating TOASTPOS " + StoreId + " Fullname CSV Files.....");
            filename = GenerateCSV.GenerateCSVFile(full, "FULLNAME", StoreId, folderPath);
            Console.WriteLine("Fullname File Generated For TOASTPOS " + StoreId);
        }
        public int getpack(string prodName)
        {
            prodName = prodName.ToUpper();
            var regexMatch = Regex.Match(prodName, @"(?<Result>\d+)PK");
            var prodPack = regexMatch.Groups["Result"].Value;
            if (prodPack.Length > 0)
            {
                return ParseIntValue(prodPack);
            }
            return 1;
        }
        public string getVolume(string prodName)
        {
            prodName = prodName.ToUpper();
            var regexMatch = Regex.Match(prodName, @"(?<Result>\d+)ML| (?<Result>\d+)LTR| (?<Result>\d+)OZ | (?<Result>\d+)L");
            var prodPack = regexMatch.Groups["Result"].Value;
            if (prodPack.Length > 0)
            {
                return regexMatch.ToString();
            }
            return "";
        }
        public int ParseIntValue(string val)
        {
            int outVal = 0;
            int.TryParse(val.Replace("$", ""), out outVal);
            return outVal;
        }

    }
    public class Root
    {
        public Token token { get; set; }
        public string status { get; set; }
    }

    public class Token
    {
        public string tokenType { get; set; }
        public int expiresIn { get; set; }
        public string accessToken { get; set; }
        public object refreshToken { get; set; }
    }

    public class RootQty
    {
        public string guid { get; set; }
        public string itemGuidValidity { get; set; }
        public string status { get; set; }
        public string quantity { get; set; }
        public string multiLocationId { get; set; }
        public string versionId { get; set; }
    }
    public class RootMenu
    {
        public List<Menu> menus { get; set; }
    }

    public class Menu
    {
        public string name { get; set; }
        public List<MenuGroup> menuGroups { get; set; }
    }

    public class MenuGroup
    {
        public string name { get; set; }
        public List<MenuItem> menuItems { get; set; }
        public List<MenuGroup> menuGroups { get; set; }
    }

    //public class MenuItem
    //{
    //    public string name { get; set; }
    //    public string guid { get; set; }
    //    public string description { get; set; }
    //    public decimal? price { get; set; }
    //    public bool? isDiscountable { get; set; }
    //    public string sku { get; set; }
    //}
    //public class InventoryItem
    //{
    //    public string MenuName { get; set; }
    //    public string GroupName { get; set; }
    //    public string ItemName { get; set; }
    //    public string Guid { get; set; }
    //    public string Description { get; set; }
    //    public decimal Price { get; set; }
    //    public bool? IsDiscountable { get; set; }
    //    public string Sku { get; set; }
    //}
    public class InventoryItem
    {
        public string MenuName { get; set; }     // Top-level menu
        public string GroupName1 { get; set; }   // First group
        public string GroupName2 { get; set; }   // Second-level group
        public string GroupName3 { get; set; }   // Third-level group (if any)
        public string ItemName { get; set; }
        public string Guid { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public bool? IsDiscountable { get; set; }
        public string Sku { get; set; }
    }

    public class SalesCategory
    {
        public string name { get; set; }
        public string guid { get; set; }
    }

    public class MenuItem
    {
        public string name { get; set; }
        public string guid { get; set; }
        public string description { get; set; }
        public decimal? price { get; set; }
        public bool? isDiscountable { get; set; }
        public string sku { get; set; }

        public SalesCategory salesCategory { get; set; } // <-- Added
    }

}

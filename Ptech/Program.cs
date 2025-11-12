using Ptech;
using PTech.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace PTech
{
    class Program
    {
        static void Main(string[] args)
        {
            string DeveloperId = ConfigurationManager.AppSettings["DeveloperId"];
            string Regular_response_StoreIds = ConfigurationManager.AppSettings["Regular_response"];
            try
            {
                POSSettings pOSSettings = new POSSettings();
                pOSSettings.IntializeStoreSettings();
                foreach (POSSetting current in pOSSettings.PosDetails)
                {
                    try
                    {
                        /*if (current.StoreSettings.StoreId == 12402)   
                        { }
                        else { continue; }*/
                        if (current.PosName.ToUpper() == "PTECH")
                        {
                            Console.WriteLine("StoreId: " + current.StoreSettings.StoreId);
                            if (Regular_response_StoreIds.Contains(current.StoreSettings.StoreId.ToString()))//specific stores
                            {
                                clsPTECH.PtechPos ptech = new clsPTECH.PtechPos(current.StoreSettings.StoreId, current.StoreSettings.POSSettings.tax, current.StoreSettings.POSSettings.BaseUrl, current.StoreSettings.POSSettings.Username, current.StoreSettings.POSSettings.Password, current.StoreSettings.POSSettings.IsMarkUpPrice, current.StoreSettings.POSSettings.MarkUpValue);
                                Console.WriteLine();
                            }
                            else if (string.IsNullOrEmpty(current.StoreSettings.POSSettings.Pin))//Added on 13-Oct-2025
                            {
                                clsPTECH.PtechPos ptech = new clsPTECH.PtechPos(current.StoreSettings.StoreId, current.StoreSettings.POSSettings.tax, current.StoreSettings.POSSettings.BaseUrl, current.StoreSettings.POSSettings.Username, current.StoreSettings.POSSettings.Password, current.StoreSettings.POSSettings.IsMarkUpPrice, current.StoreSettings.POSSettings.MarkUpValue);
                                Console.WriteLine();
                            }
                            else
                            {
                                SypramSoftware CLSPTECH = new SypramSoftware(current.StoreSettings.StoreId, current.StoreSettings.POSSettings.tax, current.StoreSettings.POSSettings.BaseUrl, current.StoreSettings.POSSettings.Username, current.StoreSettings.POSSettings.Password, current.StoreSettings.POSSettings.Pin, current.StoreSettings.POSSettings.IsMarkUpPrice, current.StoreSettings.POSSettings.MarkUpValue);
                                Console.WriteLine();
                            }
                        }
                        else if (current.PosName.ToUpper() == "SYPRAMSOFTWARE")
                        {
                            Console.WriteLine("StoreId: " + current.StoreSettings.StoreId);
                            SypramSoftware clsSypram = new SypramSoftware(current.StoreSettings.StoreId, current.StoreSettings.POSSettings.tax, current.StoreSettings.POSSettings.BaseUrl, current.StoreSettings.POSSettings.Username, current.StoreSettings.POSSettings.Password, current.StoreSettings.POSSettings.Pin, current.StoreSettings.POSSettings.IsMarkUpPrice, current.StoreSettings.POSSettings.MarkUpValue);
                            Console.WriteLine();
                        }
                        //else if (current.PosName.ToUpper() == "TOAST")
                        //{
                        //    Console.WriteLine("StoreId: " + current.StoreSettings.StoreId);
                        //    clsToast toast = new clsToast(current.StoreSettings.StoreId, current.StoreSettings.POSSettings.tax, current.StoreSettings.POSSettings.BaseUrl, current.StoreSettings.POSSettings.ClientId, current.StoreSettings.POSSettings.ClientSecret, current.StoreSettings.POSSettings.Type, current.StoreSettings.POSSettings.GUID, current.StoreSettings.POSSettings.Loyalty);
                        //    string status = toast.run();
                        //    Console.WriteLine(status);
                        //}
                        else if (current.StoreSettings.StoreId== 12381)//current.PosName.ToUpper() == "Scotch"
                        {
                            Console.WriteLine("StoreId: " + current.StoreSettings.StoreId);
                            Scotch s = new Scotch(12381, 0, "https://app.scotchpos.com", "0199168b-47ad-7882-83ff-8b30cd9aab52", "sctchps_prd_56c2868cf65b6beb171618d4c700c2cd");
                            s.RunAsync().GetAwaiter().GetResult();
                        }
                        else if (current.PosName.ToUpper() == "SHOPIFY")
                        {
                            Console.WriteLine("StoreId: " + current.StoreSettings.StoreId);
                            Shopifypos.Shopify shopify = new Shopifypos.Shopify(current.StoreSettings.StoreId, current.StoreSettings.POSSettings.tax, current.StoreSettings.POSSettings.BaseUrl, current.StoreSettings.POSSettings.accessToken);
                            Console.WriteLine();
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                new clsEmail().sendEmail(DeveloperId, "", "", "Error in PTECHPOS@" + DateTime.UtcNow + " GMT", ex.Message + "<br/>" + ex.StackTrace);
                Console.WriteLine(ex.Message);
            }
            finally
            {
            }
        }
    }
}


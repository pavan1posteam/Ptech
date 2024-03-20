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
                        if (current.PosName.ToUpper() == "PTECH")
                        {

                            if (Regular_response_StoreIds.Contains(current.StoreSettings.StoreId.ToString()))//specific stores
                            {
                                clsPTECH.PtechPos ptech = new clsPTECH.PtechPos(current.StoreSettings.StoreId, current.StoreSettings.POSSettings.tax, current.StoreSettings.POSSettings.BaseUrl, current.StoreSettings.POSSettings.Username, current.StoreSettings.POSSettings.Password, current.StoreSettings.POSSettings.IsMarkUpPrice, current.StoreSettings.POSSettings.MarkUpValue);
                                Console.WriteLine();
                            }
                            else
                            {
                                PTech.clsPTECH.PTECHCsvProducts CLSPTECH = new PTech.clsPTECH.PTECHCsvProducts(current.StoreSettings.StoreId, current.StoreSettings.POSSettings.tax, current.StoreSettings.POSSettings.BaseUrl, current.StoreSettings.POSSettings.Username, current.StoreSettings.POSSettings.Password, current.StoreSettings.POSSettings.Pin, current.StoreSettings.POSSettings.IsMarkUpPrice, current.StoreSettings.POSSettings.MarkUpValue);
                                Console.WriteLine();
                            }
                        }
                        else if (current.PosName.ToUpper() == "SYPRAMSOFTWARE")
                        {
                            PTech.clsPTECH.PTECHCsvProducts CLSPTECH = new PTech.clsPTECH.PTECHCsvProducts(current.StoreSettings.StoreId, current.StoreSettings.POSSettings.tax, current.StoreSettings.POSSettings.BaseUrl, current.StoreSettings.POSSettings.Username, current.StoreSettings.POSSettings.Password, current.StoreSettings.POSSettings.Pin, current.StoreSettings.POSSettings.IsMarkUpPrice, current.StoreSettings.POSSettings.MarkUpValue);
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


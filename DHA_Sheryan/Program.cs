using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHA_Sheryan
{
    class Program
    {
        static void Main(string[] args)
        {
            Sheryan yo = new Sheryan();





            yo.run();
            //tester.CheckDate();
            //HAHAH
        }
    }


    class tester
    {
        public static void CheckDate()
        {
            try
            {

                //00104874
                //CheckComma(ConvertDate(PPLicense_Array[0]["license_last_renewed_date"].ToString()));

                string licenselastreneweddate = "2018-05-31 00:00:00.000";

                List<string> expirydates = new List<string>();
               

                string expiry1 = "2019-05-30 00:00:00.000";
                expiry1 = ConvertDate(expiry1);
                expiry1 = CheckComma(expiry1);

                string expiry2 = "2019-01-14 00:00:00.000";
                expiry2 = ConvertDate(expiry2);
                expiry2 = CheckComma(expiry2);

                expirydates.Add(expiry1);
                expirydates.Add(expiry2);

                string maxdate = GetMaxExpiryDate(expirydates);
                maxdate = ConvertDate(maxdate);
                maxdate = CheckComma(maxdate);

                var result = ConvertDate(licenselastreneweddate);
                result = CheckComma(result);
                Console.WriteLine("Converted Renewd Date: " + result);
                Console.Read();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.Read();
            }
        }

        public static string GetMaxExpiryDate(List<string> list)
        {
            string result = string.Empty;
            DateTime date = new DateTime();
            try
            {
                foreach (string dates in list)
                {
                    if (date < Convert.ToDateTime(dates))
                    {
                        date = Convert.ToDateTime(dates);
                        result = date.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                CustomLog.Info(ex.Message);
            }

            return result;
        }
        public static string ConvertDate(string Rdate)
        {

            string[] formats = {
                        "dd/MM/yyyy hh:mm:ss tt"
                     ,  "dd/MM/yyyy hh:mm:ss"
                     ,  "dd/MM/yyyy hh:mm:ss t"
                     ,  "dd/MM/yyyy h:m:s t"
                     ,  "dd/MM/yyyy HH:mm:ss"
                     ,  "dd/MM/yyyy HH:mm:ss tt"
                     ,  "dd/MM/yyyy H:m:s t"
                     ,  "dd/MM/yyyy"
                     ,  "d/M/yy hh:mm:ss tt"
                     ,  "M/d/yyyy h:mm:ss tt"
                     ,  "M/d/yyyy h:mm tt"
                     ,  "MM/dd/yyyy hh:mm:ss"
                     ,  "M/d/yyyy h:mm:ss"
                     ,  "M/d/yyyy hh:mm tt"
                     ,  "M/d/yyyy hh tt"
                     ,  "M/d/yyyy h:mm"
                     ,  "M/d/yyyy h:mm"
                     ,  "MM/dd/yyyy hh:mm"
                     ,  "M/dd/yyyy hh:mm"
                     ,  "dd-MM-yyyy"
                     ,  "dd-MM-yyyy HH:mm:ss"
                     ,  "yyyy-MM-dd HH:mm:ss.SSS"
              };

            //Logger.Info("Transforming date " + Rdate);
            string activetoDatedata = string.Empty;
            bool isconverted = false;
            DateTime getDatefromString = DateTime.Now;

            try
            {
                //CustomLog.Info("Converting Date: " + Rdate);
                if (DateTime.TryParseExact(Rdate, formats, new System.Globalization.CultureInfo("en-US"), System.Globalization.DateTimeStyles.None, out getDatefromString))
                {
                    //Logger.Info("Converted '" + Rdate + "' to " + getDatefromString + ".");
                    //CustomLog.Info("Converted Date: " + Rdate + " to date: " + getDatefromString);
                    isconverted = true;
                }
                else
                {
                    //Logger.Info("Unable to convert '" + Rdate + "' to a date.");
                    //CustomLog.Info("Not able to parse date: " + Rdate);
                }

                if (isconverted)
                {
                    activetoDatedata = getDatefromString.ToString("MM/dd/yyyy hh:mm:ss");
                }
                else
                {
                    DateTime tmpDate = DateTime.Now;
                    DateTime dtNeedParsing = DateTime.TryParse(Rdate, out tmpDate) == true ? DateTime.Parse(Rdate) : tmpDate;

                    if (tmpDate == dtNeedParsing)
                    {
                        //Logger.Info(" ~~~~~~~~ not able to parse the date correctly ~~~~~~~~~~~~ ");
                        //CustomLog.Info("Not able to parse date: " + Rdate);
                        activetoDatedata = tmpDate.ToString();
                    }
                    else
                    {
                        activetoDatedata = dtNeedParsing.ToString("MM/dd/yyyy hh:mm:ss");
                    }
                }
                //CustomLog.Info("Final Date: " + activetoDatedata);
                return activetoDatedata;
            }

            catch (Exception ex)
            {
                //CustomLog.Info(ex.Message);
                return null;
            }
        }
        private static string CheckComma(string data)
        {
            string result = string.Empty;
            result = data;
            if (data != null)
            {
                if (data.Length > 0)
                {
                    if (data.Contains(","))
                    {
                        result = data.Replace(",", " ");
                    }
                    if (result.Contains("\n"))
                    {
                        result = result.Replace("\n", "\\n");
                    }
                    if (result.Contains("\r"))
                    {
                        result = result.Replace("\r", " ");
                    }
                    if (result.Contains("'"))
                    {
                        result = result.Replace("'", "''");
                    }
                    if (result.Contains("|"))
                    {
                        result = result.Replace("|", "");
                    }
                    if (result.Contains("\t"))
                    {
                        result = result.Replace("\t", "\\t");
                    }

                }
                else
                {
                    result = "NULL";
                }
            }
            else
            {
                result = data;
            }
            return result;
        }
    }

}

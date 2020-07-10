using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;


namespace DHA_Sheryan
{
    class Sheryan
    {
        private static string baseDir = ConfigurationManager.AppSettings.Get("BaseDir");

        public void run()
        {
            string system_username = ConfigurationManager.AppSettings.Get("username");
            string system_password = ConfigurationManager.AppSettings.Get("password");
            string URL = ConfigurationManager.AppSettings.Get("URL");
            URL = URL + ConfigurationManager.AppSettings.Get("Link");
            string token = system_username + ":" + system_password;

            try
            {
                Console.WriteLine("DHA SHERYAN is now in execution");


                DateTime GP_before = DateTime.Now;
                string filename = "Sheryan_Clinicians_" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv";

                Professional_Controller(token, URL, filename);
                RemoveDuplicates(baseDir + "\\" + filename);
                string LmuFile = LMU_Parser(baseDir + "\\" + filename);
                Upload_File(baseDir + "\\" + LmuFile);

                DateTime GP_after = DateTime.Now;
                Console.WriteLine("Time taken by GetProfessional: " + (GP_after - GP_before).TotalSeconds);




                //DateTime GF_before = DateTime.Now;
                //GetFacility(token);
                //DateTime GF_after = DateTime.Now;
                //Console.WriteLine("Time taken by GetFacility: " + (GF_after - GF_before).TotalSeconds);



            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Press any key to continue");
            }

        }
        private void Professional_Controller(string token, string URL, string filename)
        {
            try
            {
                string RunbySample = ConfigurationManager.AppSettings.Get("RunBySample");
                string RunbyFiles = ConfigurationManager.AppSettings.Get("RunByFiles");
                string RunbyDate = ConfigurationManager.AppSettings.Get("RunbyDate");
                string RunByUniqueId = ConfigurationManager.AppSettings.Get("RunByUniqueId");
                string RunbyDaily = ConfigurationManager.AppSettings.Get("RunbyDaily");

                if (Convert.ToBoolean(RunbySample))
                {
                    string SampleFilename = ConfigurationManager.AppSettings.Get("AbslouteFilename");
                    GetProfessionals_BySample(SampleFilename, filename);
                }
                else if (Convert.ToBoolean(RunbyFiles))
                {
                    string directorypath = ConfigurationManager.AppSettings.Get("DirectoryPath");
                    GetProfessionals_ByFiles(directorypath, filename);
                }
                else if (Convert.ToBoolean(RunbyDate))
                {
                    DateTime start = Convert.ToDateTime(ConfigurationManager.AppSettings.Get("Str_startDate"));
                    GetProfessionalsByDate(token, URL, start, filename);
                }
                else if (Convert.ToBoolean(RunbyDaily)) 
                {
                    GetProfessionalsByDateDaily(token, URL, filename);
                }
                else if (Convert.ToBoolean(RunByUniqueId))
                {
                    Console.WriteLine("Enter CSV File path that contains the Unique Id only");
                    string path = Convert.ToString(Console.ReadLine());
                    if (File.Exists(path))
                    {
                        GetProfessionalsByUniqueId(token, URL, path, filename);
                    }
                    else
                    {
                        Console.WriteLine("Path incorrect");
                    }
                }
                else
                {
                    GetProfessionals(token, URL, filename);
                }
            }
            catch (Exception ex)
            {
                CustomLog.Info(ex.Message);
            }
        }


        #region Professionals Calling Methods
        public void GetProfessionals(string token, string URL, string filename)
        {
            string result = string.Empty;
            string data = string.Empty;

            token = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
            try
            {
                int j = 100;
                int count = 0;

                int MaxIteration = int.Parse(ConfigurationManager.AppSettings.Get("MaxID").ToString());

                for (int i = 1; i < MaxIteration; i++)
                {
                    data = "{\r\n  \"StartIndex\": \"" + i + "\",\r\n  \"EndIndex\" :\"" + j + "\"\r\n}";
                    count++;
                    Console.Clear();
                    Console.WriteLine("Iteration Number: " + count + " Index: " + i);


                    result = PostCall_ByBody(URL, data, token);
                    if (result != null)
                    {
                        Dump_Data(data, result);
                        ParseAsDHCC_Professional(i + "-" + j, result, filename);

                        i = i + 99;
                        j = i + 100;
                    }
                }
                Console.WriteLine("Get Professionals result posted successfully");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occured: " + ex.Message);
            }
        }
        public void GetProfessionalsByDate(string token, string URL, DateTime startdate, string filename)
        {
            string result = string.Empty;
            string data = string.Empty;


            token = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
            try
            {
                string Str_EndDate = DateTime.Now.ToString("yyyy-MM-dd");
                string Str_StartDate = startdate.ToString("yyyy-MM-dd");

               

                while (Convert.ToDateTime(Str_StartDate) <= DateTime.Now)
                {
                    string Str_itr = Convert.ToDateTime(Str_StartDate).AddDays(1).ToString("yyyy-MM-dd");
                    int j = 100;
                    int i = 1;
                    string ReturnCode = string.Empty;

                    do
                    {
                        data = "{\r\n \"StartIndex\": \"" + i + "\",\r\n  \"EndIndex\" :\"" + j + "\",\r\n " + "  \"StartDate\": \"" + Str_StartDate + "\",\r\n  \"EndDate\" :\"" + Str_itr + "\"\r\n}";
                        Console.Clear();
                        Console.WriteLine("StartDate: " + Str_StartDate + " EndDate: " + Str_itr);
                        result = PostCall_ByBody(URL, data, token);
                        JObject yo = JObject.Parse(result);
                        ReturnCode = yo["ReturnCode"].ToString();

                        if (yo["ReturnCode"] != null)
                        {
                            if (yo["ReturnCode"].ToString() == "00")
                            {
                                Dump_Data(data, result);
                                ParseAsDHCC_Professional(Str_StartDate + "-" + Str_itr, result, filename);
                                i = i + 99;
                                j = i + 100;
                            }
                        }
                    } while (ReturnCode == "00");

                    Str_StartDate = Convert.ToDateTime(Str_StartDate).AddDays(1).ToString("yyyy-MM-dd");

                }

                Console.WriteLine("Get Professionals result posted successfully");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occured: " + ex.Message);
                Console.Read();
            }
        }
        public void GetProfessionalsByDateDaily(string token, string URL, string filename)
        {
            string result = string.Empty;
            string data = string.Empty;


            token = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
            try
            {
                string Str_EndDate = DateTime.Now.ToString("yyyy-MM-dd");
                string Str_StartDate = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

                int j = 100;
                int i = 1;
                string ReturnCode = string.Empty;

                do
                {
                    data = "{\r\n \"StartIndex\": \"" + i + "\",\r\n  \"EndIndex\" :\"" + j + "\",\r\n " + "  \"StartDate\": \"" + Str_StartDate + "\",\r\n  \"EndDate\" :\"" + Str_EndDate + "\"\r\n}";
                    Console.Clear();
                    Console.WriteLine("StartDate: " + Str_StartDate + " EndDate: " + Str_EndDate);
                    result = PostCall_ByBody(URL, data, token);
                    JObject yo = JObject.Parse(result);
                    ReturnCode = yo["ReturnCode"].ToString();

                    if (result != null && result != "00")
                    {
                        Dump_Data(data, result);
                        ParseAsDHCC_Professional(Str_StartDate + "-" + Str_EndDate, result, filename);

                        i = i + 99;
                        j = i + 100;
                    }

                } while (ReturnCode == "00");

                //Str_StartDate = Convert.ToDateTime(Str_StartDate).AddDays(1).ToString("yyyy-MM-dd");

                Console.WriteLine("Get Professionals result posted successfully");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occured: " + ex.Message);
                Console.Read();
            }
        }
        public void GetProfessionalsByData(string token, string URL, string filename)
        {
            string result = string.Empty;
            string data = string.Empty;

            token = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
            try
            {

                data = File.ReadAllText(@"C:\Users\faisal\Desktop\files\DHA-Sheryan\Sheryan\GetProfessionals\Input_SingleID.json");
                Console.Clear();

                result = PostCall_ByBody(URL, data, token);
                if (result != null)
                {
                    Dump_Data(data, result);
                    ParseAsDHCC_Professional("", result, filename);
                }

                Console.WriteLine("Get Professionals result posted successfully");

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occured: " + ex.Message);
                Console.Read();
            }
        }
        public void GetProfessionalsByUniqueId(string token, string URL, string path, string filename)
        {
            string result = string.Empty;
            token = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
            try
            {


                string[] dataArray = File.ReadAllLines(path);
                Console.Clear();

                foreach (string data in dataArray)
                {

                    string new_data = ApplyReverseClinicianFormula(data);

                    string data_formatted = "{\r\n  \"ProfessionalID\": \"" + new_data + "\"\r\n}";
                    result = PostCall_ByBody(URL, data_formatted, token);
                    if (result != null)
                    {
                        Dump_Data(data_formatted, result);
                        ParseAsDHCC_Professional("", result, filename);
                    }

                    Console.WriteLine("Get Professionals result posted successfully");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occured: " + ex.Message);
                Console.Read();
            }
        }
        public void GetProfessionals_ByFiles(string directorypath, string filename)
        {
            try
            {
                string[] files = Directory.GetFiles(directorypath, "*.json");
                foreach (string file in files)
                {
                    string data = File.ReadAllText(file);
                    ParseAsDHCC_Professional("", data, filename);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occured: " + ex.Message);
            }
        }
        public void GetProfessionals_BySample(string filename, string filenameToParse)
        {
            try
            {
                string data = File.ReadAllText(filename);
                ParseAsDHCC_Professional("", data, filenameToParse);
                ParseAsSheyranPublicList_Professional("", data);
            }
            catch (Exception ex)
            {
                Console.Write("Exception occured in GetProfessionals_BySample !!\n" + ex.Message);
                Console.Read();
            }
        }
        #endregion

        #region Professionals Parser
        public void Dump_Data(string input, string data)
        {
            try
            {
                JObject Joutput = JObject.Parse(data);
                JObject Jinput = JObject.Parse(input);
                string time = System.DateTime.Now.ToString("yyyMMddHHmmss");
                string inputfile = baseDir + @"\GP_Request_" + time + ".json";
                string outputfile = baseDir + @"\GP_Response_" + time + ".json";

                using (StreamWriter writer = File.CreateText(inputfile))
                {
                    writer.Write(Jinput.ToString());
                }

                using (StreamWriter writer = File.CreateText(outputfile))
                {
                    writer.Write(Joutput.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occured in Dump Data: " + ex.Message);
            }
        }
        public string ParseAsDHCC_Professional(string range, string data, string filename)
        {


            if (!File.Exists(baseDir + "\\" + filename))
            {
                using (StreamWriter writer = File.CreateText(baseDir + "\\" + filename))
                {
                    writer.Write("License, name, Username, Password, Facility License, Facility Name, area, Active From, Active To,is active, source, " +
                        "Specialty ID 1,Specialty,Gender,Nationality,Email,Phone,Specialty ID 2,Specialty ID 3,type,Old License\n");
                }
            }


            try
            {



                if (data != null)
                    if (data.Length > 0)
                    {
                        JObject yo = JObject.Parse(data);
                        if (yo["ReturnCode"] != null)
                            if (yo["ReturnCode"].ToString() == "00")
                            {
                                JArray Professional = (JArray)yo["Professionals"];

                                for (int i = 0; i < Professional.Count; i++)
                                {
                                    Console.WriteLine("Line number: " + i);
                                    LMU_Professional lmu_prof = new LMU_Professional();
                                    StringBuilder Insert_LMU = new StringBuilder();



                                    try
                                    {


                                        if (yo["Professionals"][i]["Professional"].HasValues)
                                        {


                                            JObject PProfessional = JObject.Parse(yo["Professionals"][i]["Professional"].ToString());



                                            lmu_prof.ClinicianLicene = LimitString(CheckComma(PProfessional["unique_id"].ToString()), 20);
                                            lmu_prof.ClinicianIdOld = CheckComma(PProfessional["unique_id_old"].ToString());
                                            lmu_prof.ClinicianName = CheckComma(PProfessional["first_name_en"].ToString()) + " " + CheckComma(PProfessional["last_name_en"].ToString());
                                            //lmu_prof.Username = CheckComma(PProfessional["user_name"].ToString());
                                            lmu_prof.Username = "";
                                            lmu_prof.Gender = CheckComma(CheckGender(PProfessional["gender_code"].ToString()));
                                            lmu_prof.Nationality = CheckComma(PProfessional["passport_nationality_desc_en"].ToString());
                                            lmu_prof.Phone = CheckComma(PProfessional["contact_personal_phone"].ToString());
                                            lmu_prof.Email = CheckComma(PProfessional["contact_personal_email"].ToString());
                                            lmu_prof.Active = CheckActive(CheckComma(PProfessional["is_practice_allowed"].ToString()));

                                        }


                                        if (yo["Professionals"][i]["ProfessionalExperience"].HasValues)
                                        {
                                            JArray PPExperience_Array = (JArray)yo["Professionals"][i]["ProfessionalExperience"];
                                            string workenddate = PPExperience_Array[0]["work_end_date"].ToString();

                                            foreach (JObject PPExperience in PPExperience_Array)
                                            {
                                                if (PPExperience_Array.Count > 1)
                                                {
                                                    string newworkenddate = PPExperience["work_end_date"].ToString();
                                                    if(newworkenddate != null && workenddate != null)
                                                        if(newworkenddate != string.Empty && workenddate !=string.Empty)
                                                        if (Convert.ToDateTime(workenddate) < Convert.ToDateTime(newworkenddate))
                                                        {
                                                            workenddate = newworkenddate;
                                                            lmu_prof.FacilityLicense = LimitString(CheckComma(PPExperience["facility_license_number"].ToString()), 20);
                                                            lmu_prof.FacilityName = CheckComma(PPExperience["work_name_en"].ToString());
                                                            lmu_prof.Location = LimitString(CheckComma(PPExperience["work_location_city_desc_en"].ToString()), 255);
                                                        }

                                                    //if (PPExperience["license_type_code"].ToString().ToLower() == "ftl")
                                                    //{
                                                    //    lmu_prof.FacilityLicense = LimitString(CheckComma(PPExperience["facility_license_number"].ToString()), 20);
                                                    //    lmu_prof.FacilityName = CheckComma(PPExperience["work_name_en"].ToString());
                                                    //    lmu_prof.Location = LimitString(CheckComma(PPExperience["work_location_city_desc_en"].ToString()), 255);
                                                    //}
                                                }
                                                else
                                                {
                                                    lmu_prof.FacilityLicense = LimitString(CheckComma(PPExperience["facility_license_number"].ToString()), 20);
                                                    lmu_prof.FacilityName = CheckComma(PPExperience["work_name_en"].ToString());
                                                    lmu_prof.Location = LimitString(CheckComma(PPExperience["work_location_city_desc_en"].ToString()), 255);
                                                }
                                            }

                                            //lmu_prof.FacilityLicense = LimitString(CheckComma(PPExperience_Array[0]["facility_license_number"].ToString()), 20);
                                            //lmu_prof.FacilityName = CheckComma(PPExperience_Array[0]["work_name_en"].ToString());
                                            //lmu_prof.Location = LimitString(CheckComma(PPExperience_Array[0]["work_location_city_desc_en"].ToString()), 255);
                                        }



                                        if (yo["Professionals"][i]["ProfessionalLicense"].HasValues)
                                        {
                                            JArray PPLicense_Array = (JArray)yo["Professionals"][i]["ProfessionalLicense"];
                                            int PPCounter = PPLicense_Array.Count;
                                            int count = 0;
                                            List<string> Expired_List = new List<string>();


                                            foreach (JObject PPLicense in PPLicense_Array)
                                            {
                                                Expired_List.Add(CheckComma(ConvertDate(PPLicense["license_expiry_date"].ToString())));
                                                //if (count < PPCounter)
                                                //{
                                                //    if (PPCounter > 1)
                                                //    {
                                                //        if (PPLicense["license_type_code"].ToString().ToLower() == "ftl")
                                                //        {
                                                //            if (PPLicense["license_status_code"].ToString().ToUpper() == "ACT_LICP" || PPLicense["license_status_code"].ToString().ToUpper() == "ACT_AEX_LICP")
                                                //            {
                                                //                lmu_prof.ActiveFrom = CheckComma(ConvertDate(PPLicense["license_last_renewed_date"].ToString()));
                                                //                //lmu_prof.ActiveTo = CheckComma(ConvertDate(PPLicense["license_expiry_date"].ToString()));
                                                //                Expired_List.Add(CheckComma(ConvertDate(PPLicense["license_expiry_date"].ToString())));
                                                //                lmu_prof.SpecialityDescription = CheckComma(PPLicense["primary_position_speciality_desc_en"].ToString());
                                                //            }
                                                //        }
                                                //    }
                                                //    else
                                                //    {
                                                //        lmu_prof.ActiveFrom = CheckComma(ConvertDate(PPLicense["license_last_renewed_date"].ToString()));
                                                //        //lmu_prof.ActiveTo = CheckComma(ConvertDate(PPLicense["license_expiry_date"].ToString()));
                                                //        Expired_List.Add(CheckComma(ConvertDate(PPLicense["license_expiry_date"].ToString())));
                                                //        lmu_prof.SpecialityDescription = CheckComma(PPLicense["primary_position_speciality_desc_en"].ToString());
                                                //    }
                                                //    count++;
                                                //}
                                            }

                                            lmu_prof.ActiveFrom = CheckComma(ConvertDate(PPLicense_Array[0]["license_last_renewed_date"].ToString()));
                                            lmu_prof.ActiveTo = CheckComma(ConvertDate(GetMaxExpiryDate(Expired_List)));
                                            lmu_prof.SpecialityDescription = CheckComma(PPLicense_Array[0]["primary_position_speciality_desc_en"].ToString());


                                        }

                                        if (yo["Professionals"][i]["ProfessionalPositions"].HasValues)
                                        {
                                            JArray PPPostions_Array = (JArray)yo["Professionals"][i]["ProfessionalPositions"];
                                            int PPCounter = PPPostions_Array.Count;
                                            int count = 0;
                                            bool isprimary_set = false;
                                            bool secondary_set = false;
                                            bool tertiary_set = false;
                                            foreach (JObject PPPositions in PPPostions_Array)
                                            {

                                                if (PPCounter > 1)
                                                {
                                                    if (PPPositions["is_primary"].ToString() == "1")
                                                    {
                                                        lmu_prof.SpecialityId1 = LimitString(CheckComma(PPPositions["eclaim_code"].ToString()), 255);
                                                        isprimary_set = true;
                                                    }

                                                    else if (isprimary_set == false && secondary_set == false)
                                                    {
                                                        lmu_prof.SpecialityId2 = LimitString(CheckComma(PPPositions["eclaim_code"].ToString()), 255);
                                                        secondary_set = true;
                                                    }

                                                    else if (isprimary_set == false && tertiary_set == false && secondary_set == true)
                                                    {
                                                        lmu_prof.SpecialityId3 = LimitString(CheckComma(PPPositions["eclaim_code"].ToString()), 255);
                                                        tertiary_set = true;
                                                    }
                                                    count++;
                                                }

                                                else
                                                {
                                                    lmu_prof.SpecialityId1 = LimitString(CheckComma(PPPositions["eclaim_code"].ToString()), 255);
                                                }
                                            }
                                        }

                                        lmu_prof.Password = "";
                                        lmu_prof.Source = "DHA";
                                        lmu_prof.Type = "HRD";

                                    }
                                    catch (Exception ex)
                                    {
                                        CustomLog.Info("Inner loop exception at iteration " + i + " Message:" + ex.Message);
                                        Insert_LMU.Append("Inner loop exception at iteration " + i + " Message:" + ex.Message + ex.InnerException);
                                    }

                                    string seperator = ",";
                                    Insert_LMU.Append(

                                            //ConvertDate(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")) + seperator +
                                            //ConvertDate(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")) + seperator +
                                            ApplyClinicianFormula(CheckNull(lmu_prof.ClinicianLicene)) + seperator +
                                            CheckNull(lmu_prof.ClinicianName) + seperator +
                                            CheckNull(lmu_prof.Username) + seperator +
                                            CheckNull(lmu_prof.Password) + seperator +
                                            ApplyFacilityFormula(CheckNull(lmu_prof.FacilityLicense)) + seperator +
                                            CheckNull(lmu_prof.FacilityName) + seperator +
                                            CheckNull(lmu_prof.Location) + seperator +
                                            CheckNull(lmu_prof.ActiveFrom) + seperator +
                                            CheckNull(lmu_prof.ActiveTo) + seperator +
                                            CheckNull(lmu_prof.Active) + seperator +
                                            CheckNull(lmu_prof.Source) + seperator +
                                            CheckNull(lmu_prof.SpecialityId1) + seperator +
                                            CheckNull(lmu_prof.SpecialityDescription) + seperator +
                                            CheckNull(lmu_prof.Gender) + seperator +
                                            CheckNull(lmu_prof.Nationality) + seperator +
                                            CheckNull(lmu_prof.Email) + seperator +
                                            CheckNull(lmu_prof.Phone) + seperator +
                                            CheckNull(lmu_prof.SpecialityId2) + seperator +
                                            CheckNull(lmu_prof.SpecialityId3) + seperator +
                                            CheckNull(lmu_prof.Type) + seperator +
                                            CheckNull(lmu_prof.ClinicianIdOld) +
                                            "\n");


                                    File.AppendAllText(baseDir + "\\" + filename, Insert_LMU.ToString());

                                }

                            }
                    }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured in function ParseAsDHCC !!\n" + ex.Message);
            }
            return filename;
        }
        public void ParseAsDHCC_Professional_OLD(string range, string data)
        {
            StringBuilder Insert_LMU = new StringBuilder();
            if (!File.Exists(baseDir + @"\GP_DHCC.csv"))
            {
                //Insert_LMU.Append("Start Date|Range|Clinician License|Clinician Name|Username|Password|Facility License|Facility Name|Location|Active From|Active To|active|source|"
                //          + "Specilaity ID 1|Speciality Description|Gender|Nationality|Email|Phone|Speciality ID 2|Speciality ID 3|type|Clinician Id Old\n");

                Insert_LMU.Append("Start Date|End Date|Clinician License|name|Username|Password|Facility License|Facility Name|Location|from|to|Active|source|"
                    + "Specialty ID 1|Specialty Description|Gender|Nationality|Email|Phone|Specialty ID 2|Specialty ID 3|type|Old_License\n");

            }


            try
            {
                if (data != null)
                    if (data.Length > 0)
                    {
                        JObject yo = JObject.Parse(data);
                        if (yo["ReturnCode"] != null)
                            if (yo["ReturnCode"].ToString() == "00")
                            {
                                JArray Professional = (JArray)yo["Professionals"];

                                for (int i = 0; i < Professional.Count; i++)
                                {
                                    Console.WriteLine("Line number: " + i);
                                    LMU_Professional lmu_prof = new LMU_Professional();



                                    if (yo["Professionals"][i]["Professional"].HasValues)
                                    {
                                        JObject PProfessional = JObject.Parse(yo["Professionals"][i]["Professional"].ToString());
                                        lmu_prof.ClinicianLicene = LimitString(CheckComma(PProfessional["unique_id"].ToString()), 20);
                                        lmu_prof.ClinicianIdOld = CheckComma(PProfessional["unique_id_old"].ToString());
                                        lmu_prof.ClinicianName = CheckComma(PProfessional["first_name_en"].ToString()) + " " + CheckComma(PProfessional["last_name_en"].ToString());
                                        lmu_prof.Username = CheckComma(PProfessional["user_name"].ToString());
                                        lmu_prof.Gender = CheckComma(CheckGender(PProfessional["gender_code"].ToString()));
                                        lmu_prof.Nationality = CheckComma(PProfessional["passport_nationality_desc_en"].ToString());
                                        lmu_prof.Phone = CheckComma(PProfessional["contact_personal_phone"].ToString());
                                        lmu_prof.Email = LimitString(CheckComma(PProfessional["contact_personal_email"].ToString()), 70);
                                        lmu_prof.Active = CheckActive(CheckComma(PProfessional["is_practice_allowed"].ToString()));
                                    }


                                    if (yo["Professionals"][i]["ProfessionalExperience"].HasValues)
                                    {
                                        JArray PPExperience_Array = (JArray)yo["Professionals"][i]["ProfessionalExperience"];
                                        foreach (JObject PPExperience in PPExperience_Array)
                                        {
                                            if (PPExperience_Array.Count > 1)
                                            {
                                                if (PPExperience["license_type_code"].ToString().ToLower() == "ftl")
                                                {
                                                    lmu_prof.FacilityLicense = LimitString(CheckComma(PPExperience["facility_license_number"].ToString()), 20);
                                                    lmu_prof.FacilityName = CheckComma(PPExperience["work_name_en"].ToString());
                                                    lmu_prof.Location = LimitString(CheckComma(PPExperience["work_location_city_desc_en"].ToString()), 255);
                                                }
                                            }
                                            else
                                            {
                                                lmu_prof.FacilityLicense = CheckComma(PPExperience["facility_license_number"].ToString());
                                                lmu_prof.FacilityName = CheckComma(PPExperience["work_name_en"].ToString());
                                                lmu_prof.Location = CheckComma(PPExperience["work_location_city_desc_en"].ToString());
                                            }
                                        }

                                    }

                                    if (yo["Professionals"][i]["ProfessionalLicense"].HasValues)
                                    {
                                        JArray PPLicense_Array = (JArray)yo["Professionals"][i]["ProfessionalLicense"];
                                        int PPCounter = PPLicense_Array.Count;
                                        int count = 0;
                                        List<string> Expired_List = new List<string>();


                                        foreach (JObject PPLicense in PPLicense_Array)
                                        {
                                            if (count < PPCounter)
                                            {
                                                if (PPCounter > 1)
                                                {
                                                    if (PPLicense["license_type_code"].ToString().ToLower() == "ftl")
                                                    {
                                                        if (PPLicense["license_status_code"].ToString().ToUpper() == "ACT_LICP" || PPLicense["license_status_code"].ToString().ToUpper() == "ACT_AEX_LICP")
                                                        {
                                                            lmu_prof.ActiveFrom = CheckComma(ConvertDate(PPLicense["license_last_renewed_date"].ToString()));
                                                            //lmu_prof.ActiveTo = CheckComma(ConvertDate(PPLicense["license_expiry_date"].ToString()));
                                                            Expired_List.Add(CheckComma(ConvertDate(PPLicense["license_expiry_date"].ToString())));
                                                            lmu_prof.SpecialityDescription = CheckComma(PPLicense["primary_position_speciality_desc_en"].ToString());
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    lmu_prof.ActiveFrom = CheckComma(ConvertDate(PPLicense["license_last_renewed_date"].ToString()));
                                                    //lmu_prof.ActiveTo = CheckComma(ConvertDate(PPLicense["license_expiry_date"].ToString()));
                                                    Expired_List.Add(CheckComma(ConvertDate(PPLicense["license_expiry_date"].ToString())));
                                                    lmu_prof.SpecialityDescription = CheckComma(PPLicense["primary_position_speciality_desc_en"].ToString());
                                                }
                                                count++;
                                            }
                                        }

                                        lmu_prof.ActiveTo = CheckComma(ConvertDate(GetMaxExpiryDate(Expired_List)));

                                        if (lmu_prof.ActiveTo == "01/01/0001 12:00:00 AM")
                                        {

                                        }
                                    }

                                    if (yo["Professionals"][i]["ProfessionalPositions"].HasValues)
                                    {
                                        JArray PPPostions_Array = (JArray)yo["Professionals"][i]["ProfessionalPositions"];
                                        int PPCounter = PPPostions_Array.Count;
                                        int count = 0;
                                        foreach (JObject PPPositions in PPPostions_Array)
                                        {
                                            if (count == 0)
                                            {
                                                if (PPCounter > 1)
                                                {
                                                    if (PPPositions["is_primary"].ToString() == "1")
                                                    {
                                                        lmu_prof.SpecialityId1 = LimitString(CheckComma(PPPositions["eclaim_code"].ToString()), 255);
                                                    }
                                                }

                                                else
                                                {
                                                    lmu_prof.SpecialityId1 = LimitString(CheckComma(PPPositions["eclaim_code"].ToString()), 255);
                                                }
                                            }

                                            if (count == 1)
                                            {
                                                lmu_prof.SpecialityId2 = LimitString(CheckComma(PPPositions["eclaim_code"].ToString()), 255);
                                            }

                                            if (count == 2)
                                            {
                                                lmu_prof.SpecialityId3 = LimitString(CheckComma(PPPositions["eclaim_code"].ToString()), 255);
                                            }

                                            count++;
                                        }
                                    }


                                    lmu_prof.Password = "N/A";
                                    lmu_prof.Source = "DHA";
                                    //lmu_prof.SpecialityId2 = "N/A|";
                                    //lmu_prof.SpecialityId3 = "N/A|";
                                    lmu_prof.Type = "HRD";


                                    Insert_LMU.Append(

                                        ConvertDate(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")) + "|" +
                                        ConvertDate(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")) + "|" +
                                        ApplyClinicianFormula(CheckNull(lmu_prof.ClinicianLicene)) + "|" +
                                        CheckNull(lmu_prof.ClinicianName) + "|" +
                                        CheckNull(lmu_prof.Username) + "|" +
                                        CheckNull(lmu_prof.Password) + "|" +
                                        ApplyFacilityFormula(CheckNull(lmu_prof.FacilityLicense)) + "|" +
                                        CheckNull(lmu_prof.FacilityName) + "|" +
                                        CheckNull(lmu_prof.Location) + "|" +
                                        CheckNull(lmu_prof.ActiveFrom) + "|" +
                                        CheckNull(lmu_prof.ActiveTo) + "|" +
                                        CheckNull(lmu_prof.Active) + "|" +
                                        CheckNull(lmu_prof.Source) + "|" +
                                        CheckNull(lmu_prof.SpecialityId1) + "|" +
                                        CheckNull(lmu_prof.SpecialityDescription) + "|" +
                                        CheckNull(lmu_prof.Gender) + "|" +
                                        CheckNull(lmu_prof.Nationality) + "|" +
                                        CheckNull(lmu_prof.Email) + "|" +
                                        CheckNull(lmu_prof.Phone) + "|" +
                                        CheckNull(lmu_prof.SpecialityId2) + "|" +
                                        CheckNull(lmu_prof.SpecialityId3) + "|" +
                                        CheckNull(lmu_prof.Type) + "|" +
                                        CheckNull(lmu_prof.ClinicianIdOld) +
                                        "\n");

                                }

                            }
                    }

                File.AppendAllText(baseDir + @"\GP_DHCC.csv", Insert_LMU.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured in function ParseAsDHCC !!\n" + ex.Message);
            }
        }
        public void ParseAsMasterClinician_Professional(string data)
        {
            StringBuilder MasterClinician_LMU = new StringBuilder();
            MasterClinician_LMU.Append("Start Date|Clinician License|Clinician Name|Active|Specilaity ID 1|Nationality|Phone|Email|Gender|Old Unique Id|type|Source\n");
            try
            {
                if (data != null)
                    if (data.Length > 0)
                    {
                        JObject yo = JObject.Parse(data);
                        if (yo["ReturnCode"] != null)
                            if (yo["ReturnCode"].ToString() == "00")
                            {
                                JArray Professional = (JArray)yo["Professionals"];

                                for (int i = 0; i < Professional.Count; i++)
                                {
                                    Console.WriteLine("Line number: " + i);
                                    Master_Clinician MC_Parse = new Master_Clinician();

                                    if (yo["Professionals"][i]["Professional"].HasValues)
                                    {
                                        JObject PProfessional = JObject.Parse(yo["Professionals"][i]["Professional"].ToString());
                                        MC_Parse.ClinicianLicense = ApplyClinicianFormula(CheckComma(PProfessional["unique_id"].ToString()));
                                        MC_Parse.ClinicianName = CheckComma(PProfessional["first_name_en"].ToString()) + " " + CheckComma(PProfessional["last_name_en"].ToString());
                                        MC_Parse.Active = CheckActive(CheckComma(PProfessional["is_practice_allowed"].ToString()));
                                        MC_Parse.Nationality = CheckComma(PProfessional["passport_nationality_desc_en"].ToString());
                                        MC_Parse.Phone = CheckComma(PProfessional["contact_personal_phone"].ToString());
                                        MC_Parse.Email = CheckComma(PProfessional["contact_work_email"].ToString());
                                        MC_Parse.Gender = CheckComma(PProfessional["gender_desc_en"].ToString());
                                        MC_Parse.OldUniqueId = CheckComma(PProfessional["unique_id_old"].ToString());

                                    }

                                    if (yo["Professionals"][i]["ProfessionalPositions"].HasValues)
                                    {
                                        JArray PPPostions_Array = (JArray)yo["Professionals"][i]["ProfessionalPositions"];

                                        string primarySpeciality = string.Empty;
                                        List<string> secondarySpeciality = new List<string>();
                                        foreach (JObject PPPositions in PPPostions_Array)
                                        {
                                            if (PPPostions_Array.Count > 1)
                                            {
                                                if (PPPositions["is_primary"].ToString() == "1")
                                                {
                                                    primarySpeciality = CheckComma(PPPositions["eclaim_code"].ToString());
                                                }
                                                else
                                                {
                                                    secondarySpeciality.Add(CheckComma(PPPositions["eclaim_code"].ToString()));
                                                }
                                            }
                                            else
                                            {
                                                MC_Parse.SpecialityFieldId = CheckComma(PPPositions["eclaim_code"].ToString());
                                            }
                                        }

                                        if (secondarySpeciality.Count > 0)
                                        {
                                            MC_Parse.SpecialityFieldId = primarySpeciality + " (" + ListToCommaSeperated(secondarySpeciality) + ")";
                                        }

                                    }

                                    MC_Parse.Type = "N/A";
                                    MC_Parse.Source = "N/A";


                                    //Start Date| Clinician License | Clinician Name | Active | Specilaity ID 1 | Nationality | Phone | Email | Gender | Old Unique Id| type | Source

                                    MasterClinician_LMU.Append(
                                        ConvertDate(DateTime.Now.ToString("yyyy/MM/dd")) + "|" +

                                        CheckNull(MC_Parse.ClinicianLicense) + "|" +
                                        CheckNull(MC_Parse.ClinicianName) + "|" +
                                        CheckNull(MC_Parse.Active) + "|" +
                                        CheckNull(MC_Parse.SpecialityFieldId) + "|" +
                                        CheckNull(MC_Parse.Nationality) + "|" +
                                        CheckNull(MC_Parse.Phone) + "|" +
                                        CheckNull(MC_Parse.Email) + "|" +
                                        CheckNull(MC_Parse.Gender) + "|" +
                                        CheckNull(MC_Parse.OldUniqueId) + "|" +
                                        CheckNull(MC_Parse.Type) + "|" +
                                        CheckNull(MC_Parse.Source) +

                                        "\n");
                                }
                            }
                    }
                File.AppendAllText(baseDir + @"\GP_MasterClinician.csv", MasterClinician_LMU.ToString());

                //using (StreamWriter writer = File.CreateText(baseDir + @"\GP_MasterClinician_" + System.DateTime.Now.ToString("yyyMMddHHmmss") + ".csv"))
                //{
                //    writer.Write(MasterClinician_LMU.ToString());
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured in function ParseAsMasterClinician_Professional !!\n" + ex.Message);
            }
        }
        public void ParseAsSheyranPublicList_Professional(string range, string data)
        {
            StringBuilder SheryanPublicList_LMU = new StringBuilder();
            if (!File.Exists(baseDir + @"\GP_SheryanPublicList.csv"))
            {
                SheryanPublicList_LMU.Append("Start Date|Range|License_derived_eClaims_ID|UniqueID|Professional_is_practice_allowed|Professional_name|License_type|License_status|License_initial_issued_date|License_last_renewed_date|License_effective_date|License_expiry_date|Facility_license_number|Primary_facility_name|Facility_city|eClaim_specialty_code|Nationality|Work_phone|Work_email|Gender|History_status_latest_record|History_status_effective_from_date|History_status_effective_to_date|Type|Source|Facility_group_identifier\n");
            }
            try
            {
                if (data != null)
                    if (data.Length > 0)
                    {
                        JObject yo = JObject.Parse(data);
                        if (yo["ReturnCode"] != null)
                            if (yo["ReturnCode"].ToString() == "00")
                            {
                                JArray Professional = (JArray)yo["Professionals"];

                                for (int i = 0; i < Professional.Count; i++)
                                {
                                    Console.WriteLine("Line number: " + i);

                                    SPL_Professional SPL_Parse = new SPL_Professional();
                                    var List_SPL_Positions = new List<SPL_Positions>();
                                    var List_SPL_StatusHistory = new List<SPL_StatusHistory>();
                                    var List_SPL_Experience = new List<SPL_Experience>();
                                    var List_SPL_FacilityGroup = new List<SPL_FacilityGroup>();
                                    var List_SPL_License = new List<SPL_License>();
                                    var List_SPL_LicenseStatusHistory = new List<SPL_LicenseStatusHistory>();


                                    if (yo["Professionals"][i]["Professional"].HasValues)
                                    {
                                        JObject PProfessional = JObject.Parse(yo["Professionals"][i]["Professional"].ToString());
                                        SPL_Parse.UniqueID = ApplyClinicianFormula(CheckComma(PProfessional["unique_id"].ToString()));
                                        SPL_Parse.Professional_name = CheckComma(PProfessional["first_name_en"].ToString()) + " " + CheckComma(PProfessional["last_name_en"].ToString());
                                        SPL_Parse.Professional_is_practice_allowed = CheckActive(CheckComma(PProfessional["is_practice_allowed"].ToString()));
                                        SPL_Parse.Nationality = CheckComma(PProfessional["passport_nationality_desc_en"].ToString());
                                        SPL_Parse.Work_phone = CheckComma(PProfessional["contact_personal_phone"].ToString());
                                        SPL_Parse.Work_email = CheckComma(PProfessional["contact_work_email"].ToString());
                                        SPL_Parse.Gender = CheckComma(PProfessional["gender_desc_en"].ToString());
                                        SPL_Parse.Type = "Public";
                                        SPL_Parse.Source = "Sheryan";
                                        SPL_Parse.last_modified_date = CheckComma(PProfessional["last_modified_date"].ToString());
                                    }

                                    if (yo["Professionals"][i]["ProfessionalPositions"].HasValues)
                                    {
                                        JArray PPPostions_Array = (JArray)yo["Professionals"][i]["ProfessionalPositions"];
                                        foreach (JObject PPPositions in PPPostions_Array)
                                        {
                                            List_SPL_Positions.Add(new SPL_Positions
                                            {
                                                //primarySpeciality = CheckComma(PPPositions["eclaim_code"].ToString()),
                                                //professional_license_number = CheckComma(PPPositions["professional_license_number"].ToString()),
                                                eclaim_code = CheckComma(PPPositions["eclaim_code"].ToString()),
                                                //is_primary = CheckComma(PPPositions["is_primary"].ToString()),
                                                last_modified_date = CheckComma(ConvertDate(PPPositions["last_modified_date"].ToString())),
                                                unique_id = CheckComma(PPPositions["unique_id"].ToString())

                                            });
                                        }
                                    }


                                    if (yo["Professionals"][i]["ProfessionalStatusHistory"].HasValues)
                                    {
                                        JArray PPStatusHistory_Array = (JArray)yo["Professionals"][i]["ProfessionalStatusHistory"];
                                        foreach (JObject PPStatusHistory in PPStatusHistory_Array)
                                        {
                                            List_SPL_StatusHistory.Add(new SPL_StatusHistory
                                            {
                                                History_status_latest_record = CheckComma(PPStatusHistory["professional_status_desc_en"].ToString()),
                                                History_status_effective_from_date = CheckComma(ConvertDate(PPStatusHistory["professional_status_effective_from_date"].ToString())),
                                                History_status_effective_to_date = CheckComma(ConvertDate(PPStatusHistory["professional_status_effective_to_date"].ToString())),
                                                last_modified_date = CheckComma(ConvertDate(PPStatusHistory["last_modified_date"].ToString())),
                                                unique_id = CheckComma(ConvertDate(PPStatusHistory["unique_id"].ToString()))
                                            });
                                        }
                                    }

                                    if (yo["Professionals"][i]["ProfessionalExperience"].HasValues)
                                    {
                                        JArray PPExperience_Array = (JArray)yo["Professionals"][i]["ProfessionalExperience"];
                                        foreach (JObject PPExperience in PPExperience_Array)
                                        {
                                            List_SPL_Experience.Add(new SPL_Experience
                                            {
                                                unique_id = CheckComma(PPExperience["unique_id"].ToString()),
                                                //professional_license_number = CheckComma(PPExperience["professional_license_number"].ToString()),
                                                //professional_license_number_old = CheckComma(PPExperience["professional_license_number_old"].ToString()),
                                                //license_type_code = CheckComma(PPExperience["license_type_code"].ToString()),
                                                //license_status_desc_en = CheckComma(PPExperience["license_status_desc_en"].ToString()),
                                                last_modified_date = CheckComma(ConvertDate(PPExperience["last_modified_date"].ToString())),
                                                work_location_city_desc_en = CheckComma(PPExperience["work_location_city_desc_en"].ToString())
                                            });
                                        }
                                    }

                                    if (yo["Professionals"][i]["ProfessionalLicense"].HasValues)
                                    {
                                        JArray PPLicense_Array = (JArray)yo["Professionals"][i]["ProfessionalLicense"];
                                        foreach (JObject PPLicense in PPLicense_Array)
                                        {
                                            List_SPL_License.Add(new SPL_License
                                            {
                                                unique_id = CheckComma(PPLicense["unique_id"].ToString()),
                                                //professional_license_number_old = CheckComma(PPLicense["professional_license_number_old"].ToString()),
                                                License_derived_eClaims_ID = ApplyClinicianFormula(CheckComma(PPLicense["professional_license_number"].ToString())),
                                                License_type = CheckComma(PPLicense["license_type_desc_en"].ToString()),
                                                License_status = CheckComma(PPLicense["license_status_desc_en"].ToString()),
                                                License_initial_issued_date = CheckComma(ConvertDate(PPLicense["license_initial_issued_date"].ToString())),
                                                License_last_renewed_date = CheckComma(ConvertDate(PPLicense["license_last_renewed_date"].ToString())),
                                                License_expiry_date = CheckComma(ConvertDate(PPLicense["license_expiry_date"].ToString())),
                                                last_modified_date = CheckComma(ConvertDate(PPLicense["last_modified_date"].ToString())),
                                                Facility_license_number = ApplyFacilityFormula(CheckComma(PPLicense["facility_license_number"].ToString())),
                                            });

                                            if (PPLicense["ProfessionalLicenseStatusHistory"].HasValues)
                                            {
                                                JArray PPLicenseHistory_Array = (JArray)PPLicense["ProfessionalLicenseStatusHistory"];
                                                foreach (JObject PPLicenseHistory in PPLicenseHistory_Array)
                                                {
                                                    List_SPL_LicenseStatusHistory.Add(new SPL_LicenseStatusHistory
                                                    {
                                                        //professional_license_number = ApplyClinicianFormula(CheckComma(PPLicenseHistory["professional_license_number"].ToString())),
                                                        //license_status_code = CheckComma(PPLicenseHistory["license_status_code"].ToString()),
                                                        //license_status_desc_en = CheckComma(PPLicenseHistory["license_status_desc_en"].ToString()),
                                                        last_modified_date = CheckComma(ConvertDate(PPLicenseHistory["last_modified_date"].ToString())),
                                                        License_effective_date = CheckComma(ConvertDate(PPLicenseHistory["effective_date"].ToString()))
                                                    });
                                                }
                                            }
                                        }
                                    }

                                    if (yo["Professionals"][i]["ProfessionalFacilityGroup"].HasValues)
                                    {
                                        JArray PPFacilityGroup_Array = (JArray)yo["Professionals"][i]["ProfessionalFacilityGroup"];
                                        foreach (JObject PPFacilityGroup in PPFacilityGroup_Array)
                                        {
                                            List_SPL_FacilityGroup.Add(new SPL_FacilityGroup
                                            {
                                                unique_id = CheckComma(PPFacilityGroup["unique_id"].ToString()),
                                                Primary_facility_name = CheckComma(PPFacilityGroup["primary_facility_name_en"].ToString()),
                                                last_modified_date = CheckComma(ConvertDate(PPFacilityGroup["last_modified_date"].ToString())),
                                                Facility_group_identifier = CheckComma(PPFacilityGroup["facility_group_identifier"].ToString())
                                            });
                                        }
                                    }


                                    //Sort arrays by last modified date
                                    //List_SPL_Positions = List_SPL_Positions.OrderBy(x => x.last_modified_date).ToList();
                                    //List_SPL_StatusHistory = List_SPL_StatusHistory.OrderBy(x => x.last_modified_date).ToList();
                                    //List_SPL_Experience = List_SPL_Experience.OrderBy(x => x.last_modified_date).ToList();
                                    //List_SPL_FacilityGroup = List_SPL_FacilityGroup.OrderBy(x => x.last_modified_date).ToList();
                                    //List_SPL_License = List_SPL_License.OrderBy(x => x.last_modified_date).ToList();
                                    //List_SPL_LicenseStatusHistory = List_SPL_LicenseStatusHistory.OrderBy(x => x.last_modified_date).ToList();

                                    int[] Arrays = new int[6] {
                                        List_SPL_Positions.Count,
                                        List_SPL_StatusHistory.Count,
                                        List_SPL_Experience.Count,
                                        List_SPL_FacilityGroup.Count,
                                        List_SPL_License.Count,
                                        List_SPL_LicenseStatusHistory.Count
                                    };

                                    int MAX = Arrays.Max();

                                    for (int loop = 0; loop < MAX; loop++)
                                    {
                                        //StartDate
                                        SheryanPublicList_LMU.Append(ConvertDate(DateTime.Now.ToString()) + "|");

                                        //Range
                                        SheryanPublicList_LMU.Append(range + "|");

                                        //License_derived_eClaims_ID
                                        if (List_SPL_License.Count > loop)
                                        {
                                            SheryanPublicList_LMU.Append(CheckNull(List_SPL_License[loop].License_derived_eClaims_ID) + "|");
                                        }
                                        else if (List_SPL_License.Count == 0)
                                        {
                                            SheryanPublicList_LMU.Append("|");
                                        }
                                        else
                                        {
                                            SheryanPublicList_LMU.Append(CheckNull(List_SPL_License[0].License_derived_eClaims_ID) + "|");
                                        }

                                        //Unique Id, Professional Is Practice Allowed, Professional Name
                                        SheryanPublicList_LMU.Append(
                                            CheckNull(SPL_Parse.UniqueID) + "|" +
                                            CheckNull(SPL_Parse.Professional_is_practice_allowed) + "|" +
                                            CheckNull(SPL_Parse.Professional_name) + "|"
                                            );


                                        //License Type , License Status 
                                        //License Initial Issued Date, License last renewed date
                                        if (List_SPL_License.Count > loop)
                                        {
                                            SheryanPublicList_LMU.Append(
                                                CheckNull(List_SPL_License[loop].License_type) + "|" +
                                                CheckNull(List_SPL_License[loop].License_status) + "|" +
                                                CheckNull(List_SPL_License[loop].License_initial_issued_date) + "|" +
                                                CheckNull(List_SPL_License[loop].License_last_renewed_date) + "|"
                                                );
                                        }
                                        else if (List_SPL_License.Count == 0)
                                        {
                                            SheryanPublicList_LMU.Append("||||");
                                        }
                                        else
                                        {
                                            SheryanPublicList_LMU.Append(
                                                CheckNull(List_SPL_License[0].License_type) + "|" +
                                                CheckNull(List_SPL_License[0].License_status) + "|" +
                                                CheckNull(List_SPL_License[0].License_initial_issued_date) + "|" +
                                                CheckNull(List_SPL_License[0].License_last_renewed_date) + "|"
                                                );
                                        }

                                        //license effective date
                                        if (List_SPL_LicenseStatusHistory.Count > loop)
                                        {
                                            SheryanPublicList_LMU.Append(CheckNull(List_SPL_LicenseStatusHistory[loop].License_effective_date) + "|");
                                        }
                                        else if (List_SPL_LicenseStatusHistory.Count == 0)
                                        {
                                            SheryanPublicList_LMU.Append("|");
                                        }
                                        else
                                        {
                                            SheryanPublicList_LMU.Append(CheckNull(List_SPL_LicenseStatusHistory[0].License_effective_date) + "|");
                                        }

                                        //license expiry date, facility license number
                                        if (List_SPL_License.Count > loop)
                                        {
                                            SheryanPublicList_LMU.Append(
                                                CheckNull(List_SPL_License[loop].License_expiry_date) + "|" +
                                                //CheckNull(List_SPL_License[loop].License_status) + "|" +
                                                //CheckNull(List_SPL_License[loop].License_type) + "|" +
                                                CheckNull(List_SPL_License[loop].Facility_license_number) + "|"
                                                );
                                        }
                                        else if (List_SPL_License.Count == 0)
                                        {
                                            SheryanPublicList_LMU.Append("||");
                                        }
                                        else
                                        {
                                            SheryanPublicList_LMU.Append(
                                                CheckNull(List_SPL_License[0].License_expiry_date) + "|" +
                                                //CheckNull(List_SPL_License[0].License_status) + "|" +
                                                //CheckNull(List_SPL_License[0].License_type) + "|" +
                                                CheckNull(List_SPL_License[0].Facility_license_number) + "|"
                                                );
                                        }

                                        //primary facility name MISSING
                                        if (List_SPL_FacilityGroup.Count > loop)
                                        {
                                            SheryanPublicList_LMU.Append(
                                                CheckNull(List_SPL_FacilityGroup[loop].Primary_facility_name) + "|"
                                                //CheckNull(List_SPL_FacilityGroup[loop].Facility_group_identifier) + "|"
                                                );
                                        }
                                        else if (List_SPL_FacilityGroup.Count == 0)
                                        {
                                            SheryanPublicList_LMU.Append("|");
                                        }
                                        else
                                        {
                                            SheryanPublicList_LMU.Append(
                                                CheckNull(List_SPL_FacilityGroup[0].Primary_facility_name) + "|"
                                                //CheckNull(List_SPL_FacilityGroup[loop].Facility_group_identifier) + "|"
                                                );
                                        }


                                        //facility city
                                        if (List_SPL_Experience.Count > loop)
                                        {
                                            SheryanPublicList_LMU.Append(CheckNull(List_SPL_Experience[loop].work_location_city_desc_en) + "|");
                                        }
                                        else if (List_SPL_Experience.Count == 0)
                                        {
                                            SheryanPublicList_LMU.Append("|");
                                        }
                                        else
                                        {
                                            SheryanPublicList_LMU.Append(CheckNull(List_SPL_Experience[0].work_location_city_desc_en) + "|");
                                        }


                                        //eclaim speciality code
                                        if (List_SPL_Positions.Count > loop)
                                        {
                                            SheryanPublicList_LMU.Append(CheckNull(List_SPL_Positions[loop].eclaim_code) + "|");
                                        }
                                        else if (List_SPL_Positions.Count == 0)
                                        {
                                            SheryanPublicList_LMU.Append("|");
                                        }
                                        else
                                        {
                                            SheryanPublicList_LMU.Append(CheckNull(List_SPL_Positions[0].eclaim_code) + "|");
                                        }

                                        //Nationality,Work Phone,Work Email, Gender
                                        SheryanPublicList_LMU.Append(

                                            CheckNull(SPL_Parse.Nationality) + "|" +
                                            CheckNull(SPL_Parse.Work_phone) + "|" +
                                            CheckNull(SPL_Parse.Work_email) + "|" +
                                            CheckNull(SPL_Parse.Gender) + "|"
                                            );


                                        //History Status Latest Record, History Status Effective From Date, History Status Effective To Date
                                        if (List_SPL_StatusHistory.Count > loop)
                                        {
                                            SheryanPublicList_LMU.Append(
                                                CheckNull(List_SPL_StatusHistory[loop].History_status_latest_record) + "|" +
                                                CheckNull(List_SPL_StatusHistory[loop].History_status_effective_from_date) + "|" +
                                                CheckNull(List_SPL_StatusHistory[loop].History_status_effective_to_date) + "|"
                                                );
                                        }
                                        else if (List_SPL_StatusHistory.Count == 0)
                                        {
                                            SheryanPublicList_LMU.Append("|||");
                                        }
                                        else
                                        {
                                            SheryanPublicList_LMU.Append(
                                                CheckNull(List_SPL_StatusHistory[0].History_status_latest_record) + "|" +
                                                CheckNull(List_SPL_StatusHistory[0].History_status_effective_from_date) + "|" +
                                                CheckNull(List_SPL_StatusHistory[0].History_status_effective_to_date) + "|"
                                                );
                                        }

                                        //Type, Source
                                        SheryanPublicList_LMU.Append(
                                            SPL_Parse.Type + "|" +
                                            SPL_Parse.Source + "|"
                                            );


                                        //Facility Group Identifier
                                        if (List_SPL_FacilityGroup.Count > loop)
                                        {
                                            SheryanPublicList_LMU.Append(
                                                CheckNull(List_SPL_FacilityGroup[loop].Facility_group_identifier) + "|"
                                                //CheckNull(List_SPL_FacilityGroup[loop].Facility_group_identifier) + "|"
                                                );
                                        }
                                        else if (List_SPL_FacilityGroup.Count == 0)
                                        {
                                            SheryanPublicList_LMU.Append("|");
                                        }
                                        else
                                        {
                                            SheryanPublicList_LMU.Append(
                                                CheckNull(List_SPL_FacilityGroup[0].Facility_group_identifier) + "|"
                                                //CheckNull(List_SPL_FacilityGroup[loop].Facility_group_identifier) + "|"
                                                );
                                        }

                                        SheryanPublicList_LMU.Append("\n");
                                    }

                                    //string[] POS_Array = new string[List_SPL_Positions.Count];
                                    //for (int arr = 0; arr < List_SPL_Positions.Count; arr++)
                                    //{
                                    //    string data1 =
                                    //        List_SPL_Positions[arr].unique_id + "|" +
                                    //        List_SPL_Positions[arr].last_modified_date + "|" +
                                    //        List_SPL_Positions[arr].eclaim_code + "|" +
                                    //        List_SPL_Positions[arr].is_primary + "|" +
                                    //        List_SPL_Positions[arr].primarySpeciality + "|" +
                                    //        List_SPL_Positions[arr].professional_license_number + "|"
                                    //        ;
                                    //    POS_Array[arr] = data1;
                                    //}

                                    //string[] SH_Array = new string[List_SPL_StatusHistory.Count];
                                    //for(int arr = 0;arr<List_SPL_StatusHistory.Count;arr++)
                                    //{
                                    //    string data1 =
                                    //        List_SPL_StatusHistory[arr].unique_id + "|" +
                                    //        List_SPL_StatusHistory[arr].last_modified_date + "|" +
                                    //        List_SPL_StatusHistory[arr].History_status_effective_from_date + "|" +
                                    //        List_SPL_StatusHistory[arr].History_status_effective_to_date + "|" +
                                    //        List_SPL_StatusHistory[arr].History_status_latest_record + "|"
                                    //        ;
                                    //    SH_Array[arr] = data1;
                                    //}
                                    //string[] EXP_Array = new string[List_SPL_Experience.Count];
                                    //for(int arr = 0;arr<List_SPL_Experience.Count;arr++)
                                    //{
                                    //    string data1 =
                                    //        List_SPL_Experience[arr].unique_id + "|" +
                                    //        List_SPL_Experience[arr].last_modified_date + "|" +
                                    //        List_SPL_Experience[arr].professional_license_number + "|" +
                                    //        List_SPL_Experience[arr].professional_license_number_old + "|" +
                                    //        List_SPL_Experience[arr].license_type_code + "|" +
                                    //        List_SPL_Experience[arr].license_status_desc_en + "|" +
                                    //        List_SPL_Experience[arr].work_location_city_desc_en + "|"
                                    //        ;
                                    //    EXP_Array[arr] = data1;
                                    //}

                                    //string[] LIC_Array = new string[List_SPL_License.Count];
                                    //for(int arr = 0;arr<List_SPL_License.Count;arr++)
                                    //{
                                    //    string data1 =
                                    //        List_SPL_License[arr].unique_id + "|" +
                                    //        List_SPL_License[arr].last_modified_date + "|" +
                                    //        List_SPL_License[arr].License_derived_eClaims_ID + "|" +
                                    //        List_SPL_License[arr].professional_license_number_old + "|" +
                                    //        List_SPL_License[arr].License_status + "|" +
                                    //        List_SPL_License[arr].License_type + "|" +
                                    //        List_SPL_License[arr].License_last_renewed_date + "|" +
                                    //        List_SPL_License[arr].License_initial_issued_date + "|" +
                                    //        List_SPL_License[arr].License_expiry_date + "|" +
                                    //        List_SPL_License[arr].Facility_license_number + "|"
                                    //        ;
                                    //    LIC_Array[arr] = data1;
                                    //}

                                    //string[] LSH_Array = new string[List_SPL_LicenseStatusHistory.Count];
                                    //for(int arr= 0;arr<List_SPL_LicenseStatusHistory.Count;arr++)
                                    //{
                                    //    string data1 =
                                    //        List_SPL_LicenseStatusHistory[arr].last_modified_date + "|" +
                                    //        List_SPL_LicenseStatusHistory[arr].License_effective_date + "|" +
                                    //        List_SPL_LicenseStatusHistory[arr].license_status_code + "|" +
                                    //        List_SPL_LicenseStatusHistory[arr].license_status_desc_en + "|" +
                                    //        List_SPL_LicenseStatusHistory[arr].professional_license_number + "|"
                                    //        ;
                                    //    LSH_Array[arr] = data1;
                                    //}

                                    //string[] FG_Array = new string[List_SPL_FacilityGroup.Count];
                                    //for(int arr = 0;arr<List_SPL_FacilityGroup.Count;arr++)
                                    //{
                                    //    string data1 =
                                    //        List_SPL_FacilityGroup[arr].unique_id + "|" +
                                    //        List_SPL_FacilityGroup[arr].last_modified_date + "|" +
                                    //        List_SPL_FacilityGroup[arr].Primary_facility_name + "|" +
                                    //        List_SPL_FacilityGroup[arr].Facility_group_identifier + "|"
                                    //        ;
                                    //    FG_Array[arr] = data1;
                                    //}








                                    //SheryanPublicList_LMU.Append(
                                    //    ConvertDate(DateTime.Now.ToString("yyyy/MM/dd")) + "|" +
                                    //    CheckNull(SPL_Parse.License_derived_eClaims_ID) + "|" +
                                    //    CheckNull(SPL_Parse.UniqueID) + "|" +
                                    //    CheckNull(SPL_Parse.Professional_is_practice_allowed) + "|" +
                                    //    CheckNull(SPL_Parse.Professional_name) + "|" +
                                    //    CheckNull(SPL_Parse.License_type) + "|" +
                                    //    CheckNull(SPL_Parse.License_status) + "|" +
                                    //    CheckNull(SPL_Parse.License_initial_issued_date) + "|" +
                                    //    CheckNull(SPL_Parse.License_last_renewed_date) + "|" +
                                    //    CheckNull(SPL_Parse.License_effective_date) + "|" +
                                    //    CheckNull(SPL_Parse.License_expiry_date) + "|" +
                                    //    CheckNull(SPL_Parse.Facility_license_number) + "|" +
                                    //    CheckNull(SPL_Parse.Primary_facility_name) + "|" +
                                    //    CheckNull(SPL_Parse.Facility_city) + "|" +
                                    //    CheckNull(SPL_Parse.eClaim_specialty_code) + "|" +
                                    //    CheckNull(SPL_Parse.Nationality) + "|" +
                                    //    CheckNull(SPL_Parse.Work_phone) + "|" +
                                    //    CheckNull(SPL_Parse.Work_email) + "|" +
                                    //    CheckNull(SPL_Parse.Gender) + "|" +
                                    //    CheckNull(SPL_Parse.History_status_latest_record) + "|" +
                                    //    CheckNull(SPL_Parse.History_status_effective_from_date) + "|" +
                                    //    CheckNull(SPL_Parse.History_status_effective_to_date) + "|" +
                                    //    CheckNull(SPL_Parse.Type) + "|" +
                                    //    CheckNull(SPL_Parse.Source) + "|" +
                                    //    CheckNull(SPL_Parse.Facility_group_identifier) +
                                    //    "\n"
                                    //    );
                                }
                            }
                    }

                File.AppendAllText(baseDir + @"\GP_SheryanPublicList.csv", SheryanPublicList_LMU.ToString());

                //using (StreamWriter writer = File.CreateText(baseDir + @"\GP_SheryanPublicList_" + System.DateTime.Now.ToString("yyyMMddHHmmss") + ".csv"))
                //{
                //    writer.Write(SheryanPublicList_LMU.ToString());
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured in function ParseAsMasterClinician_Professional !!\n" + ex.Message);
            }
        }
        public void ParseAsSheyranPublicList_Professional_OLD(string data)
        {
            StringBuilder SheryanPublicList_LMU = new StringBuilder();
            if (!File.Exists(baseDir + @"\GP_SheryanPublicList.csv"))
            {
                SheryanPublicList_LMU.Append("Start Date|License_derived_eClaims_ID|UniqueID|Professional_is_practice_allowed|Professional_name|License_type|License_status|License_initial_issued_date|License_last_renewed_date|License_effective_date|License_expiry_date|Facility_license_number|Primary_facility_name|Facility_city|eClaim_specialty_code|Nationality|Work_phone|Work_email|Gender|History_status_latest_record|History_status_effective_from_date|History_status_effective_to_date|Type|Source|Facility_group_identifier\n");
            }
            try
            {
                if (data != null)
                    if (data.Length > 0)
                    {
                        JObject yo = JObject.Parse(data);
                        if (yo["ReturnCode"] != null)
                            if (yo["ReturnCode"].ToString() == "00")
                            {
                                JArray Professional = (JArray)yo["Professionals"];

                                for (int i = 0; i < Professional.Count; i++)
                                {
                                    Console.WriteLine("Line number: " + i);
                                    SheryanPublicList SPL_Parse = new SheryanPublicList();
                                    List<SheryanPublicList> SPL_list = new List<SheryanPublicList>();

                                    if (yo["Professionals"][i]["Professional"].HasValues)
                                    {
                                        JObject PProfessional = JObject.Parse(yo["Professionals"][i]["Professional"].ToString());
                                        SPL_Parse.UniqueID = ApplyClinicianFormula(CheckComma(PProfessional["unique_id"].ToString()));
                                        SPL_Parse.Professional_name = CheckComma(PProfessional["first_name_en"].ToString()) + " " + CheckComma(PProfessional["last_name_en"].ToString());
                                        SPL_Parse.Professional_is_practice_allowed = CheckActive(CheckComma(PProfessional["is_practice_allowed"].ToString()));
                                        SPL_Parse.Nationality = CheckComma(PProfessional["passport_nationality_desc_en"].ToString());
                                        SPL_Parse.Work_phone = CheckComma(PProfessional["contact_personal_phone"].ToString());
                                        SPL_Parse.Work_email = CheckComma(PProfessional["contact_work_email"].ToString());
                                        SPL_Parse.Gender = CheckComma(PProfessional["gender_desc_en"].ToString());

                                        //SPL_list.Add(SPL_Parse);
                                    }

                                    if (yo["Professionals"][i]["ProfessionalPositions"].HasValues)
                                    {
                                        JArray PPPostions_Array = (JArray)yo["Professionals"][i]["ProfessionalPositions"];

                                        string primarySpeciality = string.Empty;
                                        List<string> secondarySpeciality = new List<string>();
                                        foreach (JObject PPPositions in PPPostions_Array)
                                        {
                                            if (PPPostions_Array.Count > 1)
                                            {
                                                if (PPPositions["is_primary"].ToString() == "1")
                                                {
                                                    primarySpeciality = CheckComma(PPPositions["eclaim_code"].ToString());
                                                }
                                                else
                                                {
                                                    secondarySpeciality.Add(CheckComma(PPPositions["eclaim_code"].ToString()));
                                                }
                                            }
                                            else
                                            {
                                                SPL_Parse.eClaim_specialty_code = CheckComma(PPPositions["eclaim_code"].ToString());
                                            }
                                        }

                                        if (secondarySpeciality.Count > 0)
                                        {
                                            SPL_Parse.eClaim_specialty_code = primarySpeciality + " (" + ListToCommaSeperated(secondarySpeciality) + ")";
                                        }
                                    }


                                    if (yo["Professionals"][i]["ProfessionalStatusHistory"].HasValues)
                                    {
                                        JArray PPStatusHistory_Array = (JArray)yo["Professionals"][i]["ProfessionalStatusHistory"];
                                        foreach (JObject PPStatusHistory in PPStatusHistory_Array)
                                        {
                                            if (PPStatusHistory_Array.Count > 1)
                                            {
                                                Console.WriteLine("Hello");
                                            }
                                            SPL_Parse.History_status_latest_record = CheckComma(PPStatusHistory["professional_status_desc_en"].ToString());
                                            SPL_Parse.History_status_effective_from_date = CheckComma(ConvertDate(PPStatusHistory["professional_status_effective_from_date"].ToString()));
                                            SPL_Parse.History_status_effective_to_date = CheckComma(ConvertDate(PPStatusHistory["professional_status_effective_to_date"].ToString()));
                                            //SPL_list.Add(SPL_Parse);
                                        }

                                    }

                                    if (yo["Professionals"][i]["ProfessionalExperience"].HasValues)
                                    {
                                        JArray PPExperience_Array = (JArray)yo["Professionals"][i]["ProfessionalExperience"];
                                        foreach (JObject PPExperience in PPExperience_Array)
                                        {
                                            int ai = 0;
                                            ai++;
                                            if (ai == 2)
                                            {
                                                Console.WriteLine("Gotcha!");
                                            }
                                            SPL_Parse.Facility_city = CheckComma(PPExperience["work_location_city_desc_en"].ToString());
                                        }
                                    }

                                    if (yo["Professionals"][i]["ProfessionalLicense"].HasValues)
                                    {
                                        JArray PPLicense_Array = (JArray)yo["Professionals"][i]["ProfessionalLicense"];
                                        foreach (JObject PPLicense in PPLicense_Array)
                                        {
                                            if (PPLicense_Array.Count > 1)
                                            {
                                                Console.WriteLine("Hello");
                                            }
                                            SPL_Parse.License_derived_eClaims_ID = ApplyClinicianFormula(CheckComma(PPLicense["professional_license_number"].ToString()));
                                            SPL_Parse.License_type = CheckComma(PPLicense["license_type_desc_en"].ToString());
                                            SPL_Parse.License_status = CheckComma(PPLicense["license_status_desc_en"].ToString());
                                            SPL_Parse.License_initial_issued_date = CheckComma(ConvertDate(PPLicense["license_initial_issued_date"].ToString()));
                                            SPL_Parse.License_last_renewed_date = CheckComma(ConvertDate(PPLicense["license_last_renewed_date"].ToString()));
                                            SPL_Parse.License_expiry_date = CheckComma(ConvertDate(PPLicense["license_expiry_date"].ToString()));
                                            SPL_Parse.Facility_license_number = ApplyFacilityFormula(CheckComma(PPLicense["facility_license_number"].ToString()));

                                            //SPL_list.Add(SPL_Parse);


                                            if (PPLicense["ProfessionalLicenseStatusHistory"].HasValues)
                                            {
                                                JArray PPLicenseHistory_Array = (JArray)PPLicense["ProfessionalLicenseStatusHistory"];
                                                foreach (JObject PPLicenseHistory in PPLicenseHistory_Array)
                                                {
                                                    if (PPLicenseHistory_Array.Count > 1)
                                                    {
                                                        Console.WriteLine("Hello");
                                                    }
                                                    SPL_Parse.License_effective_date = CheckComma(ConvertDate(PPLicenseHistory["effective_date"].ToString()));
                                                }
                                            }
                                        }
                                    }

                                    if (yo["Professionals"][i]["ProfessionalFacilityGroup"].HasValues)
                                    {
                                        JArray PPFacilityGroup_Array = (JArray)yo["Professionals"][i]["ProfessionalFacilityGroup"];
                                        foreach (JObject PPFacilityGroup in PPFacilityGroup_Array)
                                        {
                                            if (PPFacilityGroup_Array.Count > 1)
                                            {
                                                Console.WriteLine("Hello");
                                            }
                                            SPL_Parse.Primary_facility_name = CheckComma(PPFacilityGroup["primary_facility_name_en"].ToString());
                                            SPL_Parse.Facility_group_identifier = CheckComma(PPFacilityGroup["facility_group_identifier"].ToString());
                                        }
                                    }
                                    SPL_Parse.Type = "N/A";
                                    SPL_Parse.Source = "N/A";

                                    SheryanPublicList_LMU.Append(
                                        ConvertDate(DateTime.Now.ToString("yyyy/MM/dd")) + "|" +
                                        CheckNull(SPL_Parse.License_derived_eClaims_ID) + "|" +
                                        CheckNull(SPL_Parse.UniqueID) + "|" +
                                        CheckNull(SPL_Parse.Professional_is_practice_allowed) + "|" +
                                        CheckNull(SPL_Parse.Professional_name) + "|" +
                                        CheckNull(SPL_Parse.License_type) + "|" +
                                        CheckNull(SPL_Parse.License_status) + "|" +
                                        CheckNull(SPL_Parse.License_initial_issued_date) + "|" +
                                        CheckNull(SPL_Parse.License_last_renewed_date) + "|" +
                                        CheckNull(SPL_Parse.License_effective_date) + "|" +
                                        CheckNull(SPL_Parse.License_expiry_date) + "|" +
                                        CheckNull(SPL_Parse.Facility_license_number) + "|" +
                                        CheckNull(SPL_Parse.Primary_facility_name) + "|" +
                                        CheckNull(SPL_Parse.Facility_city) + "|" +
                                        CheckNull(SPL_Parse.eClaim_specialty_code) + "|" +
                                        CheckNull(SPL_Parse.Nationality) + "|" +
                                        CheckNull(SPL_Parse.Work_phone) + "|" +
                                        CheckNull(SPL_Parse.Work_email) + "|" +
                                        CheckNull(SPL_Parse.Gender) + "|" +
                                        CheckNull(SPL_Parse.History_status_latest_record) + "|" +
                                        CheckNull(SPL_Parse.History_status_effective_from_date) + "|" +
                                        CheckNull(SPL_Parse.History_status_effective_to_date) + "|" +
                                        CheckNull(SPL_Parse.Type) + "|" +
                                        CheckNull(SPL_Parse.Source) + "|" +
                                        CheckNull(SPL_Parse.Facility_group_identifier) +
                                        "\n"
                                        );
                                }
                            }
                    }

                File.AppendAllText(baseDir + @"\GP_SheryanPublicList.csv", SheryanPublicList_LMU.ToString());

                //using (StreamWriter writer = File.CreateText(baseDir + @"\GP_SheryanPublicList_" + System.DateTime.Now.ToString("yyyMMddHHmmss") + ".csv"))
                //{
                //    writer.Write(SheryanPublicList_LMU.ToString());
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured in function ParseAsMasterClinician_Professional !!\n" + ex.Message);
            }
        }

        #endregion

        #region LMUParser
        public static string LMU_Parser(string filepath)
        {
            string newfilename = Path.GetFileNameWithoutExtension(filepath);
            newfilename = newfilename + "_LMUPassed.csv";

            try
            {
                string[] lines = File.ReadAllLines(filepath);

                StringBuilder sb = new StringBuilder();
                sb.Append("License,name,Username,Password,Facility License,Facility Name,area,Active From,Active To,is active,source," +
                            "Specialty ID 1,Specialty,Gender,Nationality,Email,Phone,Specialty ID 2,Specialty ID 3,type,Old License\n");

                string latestversion = GetLMULatest();
                string Specialitiesversion = GetLMUSpecialitesLatest();

                if (lines.Length > 0)
                {
                    for (int i = 1; i < lines.Length; i++)
                    {


                        string[] data = lines[i].Split(',');


                        string license = data[0];
                        string license_start = data[7];
                        string license_end = data[8];
                        string isActive = data[9];
                        string source = data[10];

                        string SpecialityID = data[11];
                        string Speciality = data[12];

                        Console.WriteLine("LMU iteration " + i + " clinician license " + license);



                        GateParams obj = GetClinicianRecord(license, latestversion);
                        obj.DHA_Input = CheckActive_Reverse(isActive);
                        obj = GetTruthTable(obj);
                        isActive = obj.Out_isActive;
                        source = obj.Out_Source;
                        Speciality = GetLMURecordForSpecialities(SpecialityID, Specialitiesversion);

                        license_start = ConvertDate_LMU(license_start);
                        license_end = ConvertDate_LMU(license_end);



                        sb.Append(
                            data[0] + "," +
                            data[1] + "," +
                            data[2] + "," +
                            data[3] + "," +
                            data[4] + "," +
                            data[5] + "," +
                            data[6] + "," +
                            license_start + "," +
                            license_end + "," +
                            isActive + "," +
                            source + "," +
                            data[11] + "," +
                            Speciality + "," +
                            data[13] + "," +
                            data[14] + "," +
                            data[15] + "," +
                            data[16] + "," +
                            data[17] + "," +
                            data[18] + "," +
                            data[19] + "," +
                            data[20] + "\n"
                            );
                    }


                    Console.WriteLine("Writting file");


                    using (StreamWriter wrtier = File.CreateText(baseDir + "\\" + newfilename))
                    {
                        wrtier.Write(sb.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return newfilename;
        }
        public static string GetLMULatest()
        {
            string result = string.Empty;
            try
            {
                string url = ConfigurationManager.AppSettings.Get("LMU_URL") + ConfigurationManager.AppSettings.Get("LMU_Clinician_Latest");
                string username = ConfigurationManager.AppSettings.Get("LMU_Prod_Username");
                string token = ConfigurationManager.AppSettings.Get("LMU_Prod_Token");
                result = PostCall_ByBody(url, "", token, username,false);
                //result = "7539";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return result;
        }
        public static string GetLMUSpecialitesLatest()
        {
            string result = string.Empty;
            try
            {
                string url = ConfigurationManager.AppSettings.Get("LMU_URL") + ConfigurationManager.AppSettings.Get("LMU_Specialities_Latest");
                string username = ConfigurationManager.AppSettings.Get("LMU_Prod_Username");
                string token = ConfigurationManager.AppSettings.Get("LMU_Prod_Token");
                result = PostCall_ByBody(url, "", token, username, false);
                //result = "7539";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return result;
        }
        public static GateParams GetClinicianRecord(string data, string latestversion)
        {
            string result = string.Empty;
            GateParams obj = new GateParams();

            try
            {
                string url = ConfigurationManager.AppSettings.Get("LMU_URL") + ConfigurationManager.AppSettings.Get("LMU_Clinician"); ;
                string username = ConfigurationManager.AppSettings.Get("LMU_Prod_Username");
                string token = ConfigurationManager.AppSettings.Get("LMU_Prod_Token");
                string license = data;


                //string body = "{\r\n \"oldVersion\": 0,\"targetVersion\": " + GetLMULatest() + ",\r\n \"param\" : \"license=" + license + "\" \r\n}";
                //string body = "{\r\n \"oldVersion\": 0,\"targetVersion\": " + latestversion + ",\r\n \"param\" : \"license=" + license + "\" \r\n}";

                result = PostCall_ByBody(url, data, token, username,true);



                JObject yo = JObject.Parse(result);
                if (yo["content"] != null)
                {
                    if (yo["content"].First != null)
                    {
                        string status = yo["content"].First["status"].ToString();
                        string isActive = yo["content"].First["values"]["isActive"].ToString();
                        string source = yo["content"].First["values"]["source"].ToString();

                        obj.LMU_isActive = isActive;
                        obj.LMU_Source = source;
                        obj.LMU_Status = status;
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return obj;
        }
        public static string GetLMURecordForSpecialities(string data,string Specialitiesversion)
        {
            string result = string.Empty;
            string description = string.Empty;
            try
            {
                string url = ConfigurationManager.AppSettings.Get("LMU_URL") + ConfigurationManager.AppSettings.Get("LMU_Specialities"); ;
                string username = ConfigurationManager.AppSettings.Get("LMU_Prod_Username");
                string token = ConfigurationManager.AppSettings.Get("LMU_Prod_Token");
                string body = "{\r\n \"oldVersion\": 0,\"targetVersion\": " + Specialitiesversion + ",\r\n \"param\" : \"specialtyId=" + data + "\" \r\n}";


                if (data != null)
                {
                    if (data.Length > 1)
                    {
                        result = PostCall_ByBody(url, body, token, username, false);
                        CustomLog.Info(result);
                        JObject yo = JObject.Parse(result);
                        if (yo["content"] != null)
                        {
                            if (yo["content"].First != null)
                            {
                                string specialty = yo["content"].First["values"]["specialty"].ToString();
                                description = specialty;
                            }
                        }
                    }
                }



            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return description;
        }
        

        public static GateParams GetTruthTable(GateParams obj)
        {
            try
            {


                if (obj.LMU_isActive != null && obj.LMU_Source != null)
                {

                    if (obj.DHA_Input.ToUpper() == "TRUE" && obj.LMU_isActive.ToUpper() == "TRUE" && obj.LMU_Source.ToUpper() == "ALL")
                    {
                        obj.Out_isActive = "TRUE";
                        obj.Out_Source = "ALL";
                    }

                    else if (obj.DHA_Input.ToUpper() == "FALSE" && obj.LMU_isActive.ToUpper() == "TRUE" && obj.LMU_Source.ToUpper() == "ALL")
                    {
                        obj.Out_isActive = "TRUE";
                        obj.Out_Source = "HAAD";
                    }


                    else if (obj.DHA_Input.ToUpper() == "TRUE" && obj.LMU_isActive.ToUpper() == "FALSE" && obj.LMU_Source.ToUpper() == "ALL")
                    {
                        obj.Out_isActive = "TRUE";
                        obj.Out_Source = "DHA";
                    }

                    else if (obj.DHA_Input.ToUpper() == "TRUE" && obj.LMU_isActive.ToUpper() == "FALSE" && obj.LMU_Source.ToUpper() == "DHA")
                    {
                        obj.Out_isActive = "TRUE";
                        obj.Out_Source = "DHA";
                    }

                    else if (obj.DHA_Input.ToUpper() == "FALSE" && obj.LMU_isActive.ToUpper() == "TRUE" && obj.LMU_Source.ToUpper() == "DHA")
                    {
                        obj.Out_isActive = "FALSE";
                        obj.Out_Source = "DHA";
                    }

                    else if (obj.DHA_Input.ToUpper() == "FALSE" && obj.LMU_isActive.ToUpper() == "FALSE" && obj.LMU_Source.ToUpper() == "DHA")
                    {
                        obj.Out_isActive = "FALSE";
                        obj.Out_Source = "DHA";
                    }

                    else if (obj.DHA_Input.ToUpper() == "TRUE" && obj.LMU_isActive.ToUpper() == "TRUE" && obj.LMU_Source.ToUpper() == "DHA")
                    {
                        obj.Out_isActive = "TRUE";
                        obj.Out_Source = "DHA";
                    }

                    else if (obj.DHA_Input.ToUpper() == "TRUE" && obj.LMU_isActive.ToUpper() == "TRUE" && obj.LMU_Source.ToUpper() == "HAAD")
                    {
                        obj.Out_isActive = "TRUE";
                        obj.Out_Source = "ALL";
                    }

                    else if (obj.DHA_Input.ToUpper() == "FALSE" && obj.LMU_isActive.ToUpper() == "TRUE" && obj.LMU_Source.ToUpper() == "HAAD")
                    {
                        obj.Out_isActive = "TRUE";
                        obj.Out_Source = "HAAD";
                    }

                }


                else if (obj.DHA_Input.ToUpper() == "TRUE")
                {
                    obj.Out_isActive = "TRUE";
                    obj.Out_Source = "DHA";
                }


                else if (obj.DHA_Input.ToUpper() == "FALSE")
                {
                    obj.Out_isActive = "FALSE";
                    obj.Out_Source = "DHA";
                }




            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return obj;
        }
        public static string CheckActive_Reverse(string data)
        {
            string active = string.Empty;

            if (data.ToString() == "Deactivated")
                active = "False";
            if (data.ToString() == "Active")
                active = "True";
            else
                active = "False";

            return active;
        }
        public static string PostCall_ByBody(string URL, string postdata, string accessKey, string username,bool isGet)
        {
            string result = string.Empty;
            bool OKAY = false;
            int counter = 0;
            //int counter_limit = int.Parse(ConfigurationManager.AppSettings.Get("counter_limit"));
            int counter_limit = 1;

            try
            {
                while (!OKAY)
                {
                    try
                    {
                        using (WebClient wc = new WebClient())
                        {
                            wc.Headers[HttpRequestHeader.ContentType] = "application/json";
                            //wc.Headers[HttpRequestHeader.Authorization] = staticToken;

                            wc.Headers.Set("username", username);
                            wc.Headers.Set("access-key", accessKey);

                            CustomLog.Info("Sheryan called at " + DateTime.Now);
                            if(!isGet)
                            {
                                result = wc.UploadString(URL, postdata);
                            }
                            if (isGet)
                            {
                                string complete = URL + "?queryString=values.license|EQ|" + postdata;
                                result = wc.DownloadString(complete);
                            }

                            //CustomLog.Info(result);
                            //Console.WriteLine(result);

                            if (result != null)
                                if (result.Length > 0)
                                {
                                    //JObject yo = JObject.Parse(result);
                                    //if (yo["ReturnCode"] != null)
                                    //{
                                    //    Console.WriteLine("Return Code: " + yo["ReturnCode"].ToString());
                                    //    if (yo["ReturnCode"].ToString() == "00")
                                    //    {
                                    //        OKAY = true;
                                    //        //("Results found for data: " + postdata);
                                    //        //CustomLog.Info("Results found for data: " + postdata);
                                    //    }
                                    //    if (yo["ReturnCode"].ToString() == "10" || yo["ReturnCode"].ToString() == "07")
                                    //    {
                                    //        OKAY = true;
                                    //        //("Results found for data: " + postdata);
                                    //        //CustomLog.Info(yo["ReturnMessage"].ToString());
                                    //    }
                                    //}
                                    //else
                                    //{
                                    //    Console.WriteLine("Result: " + yo.ToString());
                                    //    //CustomLog.Info(yo.ToString());
                                    //    Console.WriteLine(yo.ToString());

                                    //}

                                    OKAY = true;

                                }
                                else if (counter > counter_limit)
                                {
                                    OKAY = true;
                                    Console.WriteLine("Tried hitting the service " + counter_limit + " times but no results found");

                                }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Write(ex.Message);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occured in PostCall_ByBody !\n" + result);
                return ex.Message;
            }
        }
        private static string ConvertDate_LMU(string date)
        {
            string resultdate = date;
            try
            {
                if (date != null)
                {
                    if (date.Length > 0)
                    {
                        resultdate = Convert.ToDateTime(date).ToString("yyyy-MM-dd");
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return resultdate;
        }
        public static void Upload_File(string local_file_path)
        {
            string ftpUserName = System.Configuration.ConfigurationManager.AppSettings["FTPUsername"];
            string ftpPassword = System.Configuration.ConfigurationManager.AppSettings["FTPPassword"];
            string FTPServerPath = System.Configuration.ConfigurationManager.AppSettings["FTPLocalPath"];
            string ftpServerIP = System.Configuration.ConfigurationManager.AppSettings["FTPHost"];
            try
            {

                CustomLog.Info("Uploading file " + local_file_path + " to address " + ftpServerIP + "/Pending/" + Path.GetFileName(local_file_path));
                string requestpath = @"ftp://" + ftpServerIP + FTPServerPath + "//" + Path.GetFileName(local_file_path);
                using (var client = new WebClient())
                {
                    client.Credentials = new NetworkCredential(ftpUserName, ftpPassword);
                    client.UploadFile(requestpath, WebRequestMethods.Ftp.UploadFile, local_file_path);
                }
            }
            catch (Exception ex)
            {
                CustomLog.Info(ex.Message);
            }

        }
        #endregion

        #region Database Parser
        public string ParseToInsert_Professional(string data)
        {
            string result = string.Empty;
            try
            {

                if (data != null)
                    if (data.Length > 0)
                    {
                        JObject yo = JObject.Parse(data);
                        if (yo["ReturnCode"] != null)
                            if (yo["ReturnCode"].ToString() == "00")
                            {
                                JArray Professional = (JArray)yo["Professionals"];

                                StringBuilder Insert_Professional = new StringBuilder();
                                Insert_Professional.Append("INSERT INTO [dbo].[GP_Professional]([unique_id],[unique_id_old],[user_name],[is_practice_allowed],[gender_code],[gender_desc_en],[gender_desc_ar],[first_name_en],[last_name_en],[emirates_id_expiry_date],[passport_number],[passport_previous_passport_number],[passport_expiry_date],[passport_country_of_birth_code],[passport_country_of_birth_desc_en],[passport_nationality_desc_en],[contact_personal_phone],[contact_personal_email],[contact_work_email],[is_premium_active],[last_modified_date]) VALUES ");

                                StringBuilder Insert_ProfessionalStatusHistory = new StringBuilder();
                                Insert_ProfessionalStatusHistory.Append("INSERT INTO [dbo].[GP_ProfessionalStatusHistory]([unique_id],[professional_status_code],[professional_status_desc_en],[professional_status_desc_ar],[professional_status_effective_from_date],[professional_status_effective_to_date],[last_modified_date]) VALUES ");

                                StringBuilder Insert_ProfessionalLicense = new StringBuilder();
                                Insert_ProfessionalLicense.Append("INSERT INTO [dbo].[GP_ProfessionalLicense]([unique_id],[professional_license_number],[professional_license_number_old],[license_type_code],[license_type_desc_en],[license_type_desc_ar],[license_status_code],[license_status_desc_en],[license_status_desc_ar],[license_initial_issued_date],[license_last_renewed_date],[license_expiry_date],[license_limited_to],[license_privilege],[license_scope_of_practice],[active_card_number],[facility_license_number],[is_medical_director],[supervisor_license_number],[primary_position_category_code],[primary_position_category_desc_en],[primary_position_category_desc_ar],[primary_position_title_code],[primary_position_title_desc_en],[primary_position_title_desc_ar],[primary_position_speciality_code],[primary_position_speciality_desc_en],[primary_position_speciality_desc_ar],[last_modified_date]) VALUES");

                                StringBuilder Insert_ProfessionalLicenseStatusHistory = new StringBuilder();
                                Insert_ProfessionalLicenseStatusHistory.Append("INSERT INTO [dbo].[GP_ProfessionalLicenseStatusHistory]([unique_id],[professional_license_number],[license_status_code],[license_status_desc_en],[license_status_desc_ar],[effective_date],[last_modified_date]) VALUES");

                                StringBuilder Insert_ProfessionalPositions = new StringBuilder();
                                Insert_ProfessionalPositions.Append("INSERT INTO [dbo].[GP_ProfessionalPositions]([unique_id],[category_code],[category_desc_en],[category_desc_ar],[title_code],[title_desc_en],[title_desc_ar],[speciality_code],[speciality_en],[speciality_ar],[card_title_en],[card_title_ar],[is_primary],[limited_to],[registration_status_code],[registration_status_desc_en],[registration_status_desc_ar],[registration_start_date],[registration_expiry_date],[professional_license_number],[eclaim_code],[last_modified_date]) VALUES ");

                                StringBuilder Insert_ProfessionalExperience = new StringBuilder();
                                Insert_ProfessionalExperience.Append("INSERT INTO [dbo].[GP_ProfessionalExperience]([unique_id],[professional_license_number],[professional_license_number_old],[license_type_code],[license_type_desc_en],[license_type_desc_ar],[license_status_code],[license_status_desc_en],[license_status_desc_ar],[license_expiry_date],[facility_license_number],[work_start_date],[work_end_date],[work_comment],[work_name_en],[work_name_ar],[work_location_city_desc_en],[work_location_city_desc_ar],[work_location_country_code],[work_location_country_desc_en],[work_location_country_desc_ar],[work_position],[last_modified_date]) VALUES ");

                                StringBuilder Insert_ProfessionalEducation = new StringBuilder();
                                Insert_ProfessionalEducation.Append("INSERT INTO [dbo].[GP_ProfessionalEducation]([unique_id],[education_type_code],[education_type_desc_en],[education_type_desc_ar],[education_title],[education_awarded_year],[issuing_entity],[issuing_authority_name],[issuing_authority_city],[issuing_authority_country_code],[issuing_authority_country_country_desc_en],[issuing_authority_country_country_desc_ar],[is_verified],[last_modified_date]) VALUES ");

                                StringBuilder Insert_ProfessionalFacilityGroup = new StringBuilder();
                                Insert_ProfessionalFacilityGroup.Append("INSERT INTO [dbo].[GP_ProfessionalFacilityGroup]([unique_id],[professional_license_number],[facility_group_identifier],[primary_facility_license_number],[primary_facility_name_en],[primary_facility_name_ar],[member_facility_license_number],[member_facility_name_en],[member_facility_name_ar],[last_modified_date]) VALUES ");

                                for (int i = 0; i < Professional.Count; i++)
                                {
                                    string professional_id = yo["Professionals"][i]["Professional"]["unique_id"].ToString();
                                    if (yo["Professionals"][i]["Professional"].HasValues)
                                    {
                                        JObject PProfessional = JObject.Parse(yo["Professionals"][i]["Professional"].ToString());
                                        Insert_Professional.Append("\n(");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["unique_id"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["unique_id_old"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["user_name"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["is_practice_allowed"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["gender_code"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["gender_desc_en"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["gender_desc_ar"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["first_name_en"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["last_name_en"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(ConvertDate(PProfessional["emirates_id_expiry_date"].ToString())) + "',");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["passport_number"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["passport_previous_passport_number"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(ConvertDate(PProfessional["passport_expiry_date"].ToString())) + "',");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["passport_country_of_birth_code"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["passport_country_of_birth_desc_en"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["passport_nationality_desc_en"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["contact_personal_phone"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["contact_personal_email"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["contact_work_email"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(PProfessional["is_premium_active"].ToString()) + "',");
                                        Insert_Professional.Append("'" + CheckComma(ConvertDate(PProfessional["last_modified_date"].ToString())) + "'");
                                        Insert_Professional.Append("),\n");
                                    }

                                    if (yo["Professionals"][i]["ProfessionalStatusHistory"].HasValues)
                                    {
                                        JArray PPStatusHistory_Array = (JArray)yo["Professionals"][i]["ProfessionalStatusHistory"];
                                        foreach (JObject PPStatusHistory in PPStatusHistory_Array)
                                        {

                                            Insert_ProfessionalStatusHistory.Append("\n(");

                                            Insert_ProfessionalStatusHistory.Append("'" + CheckComma(PPStatusHistory["unique_id"].ToString()) + "',");
                                            Insert_ProfessionalStatusHistory.Append("'" + CheckComma(PPStatusHistory["professional_status_code"].ToString()) + "',");
                                            Insert_ProfessionalStatusHistory.Append("'" + CheckComma(PPStatusHistory["professional_status_desc_en"].ToString()) + "',");
                                            Insert_ProfessionalStatusHistory.Append("'" + CheckComma(PPStatusHistory["professional_status_desc_ar"].ToString()) + "',");
                                            Insert_ProfessionalStatusHistory.Append("'" + CheckComma(ConvertDate(PPStatusHistory["professional_status_effective_from_date"].ToString())) + "',");
                                            Insert_ProfessionalStatusHistory.Append("'" + CheckComma(ConvertDate(PPStatusHistory["professional_status_effective_to_date"].ToString())) + "',");
                                            Insert_ProfessionalStatusHistory.Append("'" + CheckComma(ConvertDate(PPStatusHistory["last_modified_date"].ToString())) + "'");

                                            Insert_ProfessionalStatusHistory.Append("),\n");
                                        }

                                    }

                                    if (yo["Professionals"][i]["ProfessionalLicense"].HasValues)
                                    {
                                        JArray PPLicense_Array = (JArray)yo["Professionals"][i]["ProfessionalLicense"];
                                        foreach (JObject PPLicense in PPLicense_Array)
                                        {

                                            Insert_ProfessionalLicense.Append("\n(");

                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["unique_id"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["professional_license_number"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["professional_license_number_old"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["license_type_code"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["license_type_desc_en"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["license_type_desc_ar"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["license_status_code"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["license_status_desc_en"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["license_status_desc_ar"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(ConvertDate(PPLicense["license_initial_issued_date"].ToString())) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(ConvertDate(PPLicense["license_last_renewed_date"].ToString())) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(ConvertDate(PPLicense["license_expiry_date"].ToString())) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["license_limited_to"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["license_privilege"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["license_scope_of_practice"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["active_card_number"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["facility_license_number"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["is_medical_director"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["supervisor_license_number"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["primary_position_category_code"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["primary_position_category_desc_en"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["primary_position_category_desc_ar"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["primary_position_title_code"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["primary_position_title_desc_en"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["primary_position_title_desc_ar"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["primary_position_speciality_code"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["primary_position_speciality_desc_en"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(PPLicense["primary_position_speciality_desc_ar"].ToString()) + "',");
                                            Insert_ProfessionalLicense.Append("'" + CheckComma(ConvertDate(PPLicense["last_modified_date"].ToString())) + "'");

                                            Insert_ProfessionalLicense.Append("),\n");

                                            if (PPLicense["ProfessionalLicenseStatusHistory"].HasValues)
                                            {
                                                JArray PPLicenseHistory_Array = (JArray)PPLicense["ProfessionalLicenseStatusHistory"];
                                                foreach (JObject PPLicenseHistory in PPLicenseHistory_Array)
                                                {
                                                    Insert_ProfessionalLicenseStatusHistory.Append("\n(");
                                                    //additional field for DB Only
                                                    Insert_ProfessionalLicenseStatusHistory.Append("'" + PPLicense["unique_id"].ToString() + "',");

                                                    Insert_ProfessionalLicenseStatusHistory.Append("'" + CheckComma(PPLicenseHistory["professional_license_number"].ToString()) + "',");
                                                    Insert_ProfessionalLicenseStatusHistory.Append("'" + CheckComma(PPLicenseHistory["license_status_code"].ToString()) + "',");
                                                    Insert_ProfessionalLicenseStatusHistory.Append("'" + CheckComma(PPLicenseHistory["license_status_desc_en"].ToString()) + "',");
                                                    Insert_ProfessionalLicenseStatusHistory.Append("'" + CheckComma(PPLicenseHistory["license_status_desc_ar"].ToString()) + "',");
                                                    Insert_ProfessionalLicenseStatusHistory.Append("'" + CheckComma(ConvertDate(PPLicenseHistory["effective_date"].ToString())) + "',");
                                                    Insert_ProfessionalLicenseStatusHistory.Append("'" + CheckComma(ConvertDate(PPLicenseHistory["last_modified_date"].ToString())) + "'");

                                                    Insert_ProfessionalLicenseStatusHistory.Append("),\n");
                                                }
                                            }

                                        }

                                    }

                                    if (yo["Professionals"][i]["ProfessionalPositions"].HasValues)
                                    {
                                        JArray PPPostions_Array = (JArray)yo["Professionals"][i]["ProfessionalPositions"];
                                        foreach (JObject PPPositions in PPPostions_Array)
                                        {
                                            Insert_ProfessionalPositions.Append("\n(");

                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["unique_id"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["category_code"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["category_desc_en"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["category_desc_ar"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["title_code"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["title_desc_en"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["title_desc_ar"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["speciality_code"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["speciality_en"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["speciality_ar"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["card_title_en"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["card_title_ar"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["is_primary"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["limited_to"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["registration_status_code"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["registration_status_desc_en"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["registration_status_desc_ar"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(ConvertDate(PPPositions["registration_start_date"].ToString())) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(ConvertDate(PPPositions["registration_expiry_date"].ToString())) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["professional_license_number"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(PPPositions["eclaim_code"].ToString()) + "',");
                                            Insert_ProfessionalPositions.Append("'" + CheckComma(ConvertDate(PPPositions["last_modified_date"].ToString())) + "'");

                                            Insert_ProfessionalPositions.Append("),\n");
                                        }
                                    }

                                    if (yo["Professionals"][i]["ProfessionalFacilityGroup"].HasValues)
                                    {
                                        JArray PPFacilityGroup_Array = (JArray)yo["Professionals"][i]["ProfessionalFacilityGroup"];
                                        foreach (JObject PPFacilityGroup in PPFacilityGroup_Array)
                                        {
                                            Insert_ProfessionalFacilityGroup.Append("\n(");

                                            Insert_ProfessionalFacilityGroup.Append("'" + CheckComma(PPFacilityGroup["unique_id"].ToString()) + "',");
                                            Insert_ProfessionalFacilityGroup.Append("'" + CheckComma(PPFacilityGroup["professional_license_number"].ToString()) + "',");
                                            Insert_ProfessionalFacilityGroup.Append("'" + CheckComma(PPFacilityGroup["facility_group_identifier"].ToString()) + "',");
                                            Insert_ProfessionalFacilityGroup.Append("'" + CheckComma(PPFacilityGroup["primary_facility_license_number"].ToString()) + "',");
                                            Insert_ProfessionalFacilityGroup.Append("'" + CheckComma(PPFacilityGroup["primary_facility_name_en"].ToString()) + "',");
                                            Insert_ProfessionalFacilityGroup.Append("'" + CheckComma(PPFacilityGroup["primary_facility_name_ar"].ToString()) + "',");
                                            Insert_ProfessionalFacilityGroup.Append("'" + CheckComma(PPFacilityGroup["member_facility_license_number"].ToString()) + "',");
                                            Insert_ProfessionalFacilityGroup.Append("'" + CheckComma(PPFacilityGroup["member_facility_name_en"].ToString()) + "',");
                                            Insert_ProfessionalFacilityGroup.Append("'" + CheckComma(PPFacilityGroup["member_facility_name_ar"].ToString()) + "',");
                                            Insert_ProfessionalFacilityGroup.Append("'" + CheckComma(PPFacilityGroup["last_modified_date"].ToString()) + "'");

                                            Insert_ProfessionalFacilityGroup.Append("),\n");
                                        }
                                    }

                                    if (yo["Professionals"][i]["ProfessionalExperience"].HasValues)
                                    {
                                        JArray PPExperience_Array = (JArray)yo["Professionals"][i]["ProfessionalExperience"];
                                        foreach (JObject PPExperience in PPExperience_Array)
                                        {
                                            Insert_ProfessionalExperience.Append("\n(");

                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["unique_id"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["professional_license_number"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["professional_license_number_old"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["license_type_code"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["license_type_desc_en"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["license_type_desc_ar"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["license_status_code"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["license_status_desc_en"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["license_status_desc_ar"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(ConvertDate(PPExperience["license_expiry_date"].ToString())) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["facility_license_number"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(ConvertDate(PPExperience["work_start_date"].ToString())) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(ConvertDate(PPExperience["work_end_date"].ToString())) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["work_comment"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["work_name_en"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["work_name_ar"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["work_location_city_desc_en"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["work_location_city_desc_ar"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["work_location_country_code"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["work_location_country_desc_en"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["work_location_country_desc_ar"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(PPExperience["work_position"].ToString()) + "',");
                                            Insert_ProfessionalExperience.Append("'" + CheckComma(ConvertDate(PPExperience["last_modified_date"].ToString())) + "'");

                                            Insert_ProfessionalExperience.Append("),\n");
                                        }
                                    }

                                    if (yo["Professionals"][i]["ProfessionalEducation"].HasValues)
                                    {
                                        JArray PPEducation_Array = (JArray)yo["Professionals"][i]["ProfessionalEducation"];
                                        foreach (JObject PPEducation in PPEducation_Array)
                                        {
                                            Insert_ProfessionalEducation.Append("\n(");

                                            Insert_ProfessionalEducation.Append("'" + CheckComma(PPEducation["unique_id"].ToString()) + "',");
                                            Insert_ProfessionalEducation.Append("'" + CheckComma(PPEducation["education_type_code"].ToString()) + "',");
                                            Insert_ProfessionalEducation.Append("'" + CheckComma(PPEducation["education_type_desc_en"].ToString()) + "',");
                                            Insert_ProfessionalEducation.Append("'" + CheckComma(PPEducation["education_type_desc_ar"].ToString()) + "',");
                                            Insert_ProfessionalEducation.Append("'" + CheckComma(PPEducation["education_title"].ToString()) + "',");
                                            Insert_ProfessionalEducation.Append("'" + CheckComma(PPEducation["education_awarded_year"].ToString()) + "',");
                                            Insert_ProfessionalEducation.Append("'" + CheckComma(PPEducation["issuing_entity"].ToString()) + "',");
                                            Insert_ProfessionalEducation.Append("'" + CheckComma(PPEducation["issuing_authority_name"].ToString()) + "',");
                                            Insert_ProfessionalEducation.Append("'" + CheckComma(PPEducation["issuing_authority_city"].ToString()) + "',");
                                            Insert_ProfessionalEducation.Append("'" + CheckComma(PPEducation["issuing_authority_country_code"].ToString()) + "',");
                                            Insert_ProfessionalEducation.Append("'" + CheckComma(PPEducation["issuing_authority_country_country_desc_en"].ToString()) + "',");
                                            Insert_ProfessionalEducation.Append("'" + CheckComma(PPEducation["issuing_authority_country_country_desc_ar"].ToString()) + "',");
                                            Insert_ProfessionalEducation.Append("'" + CheckComma(PPEducation["is_verified"].ToString()) + "',");
                                            Insert_ProfessionalEducation.Append("'" + CheckComma(ConvertDate(PPEducation["last_modified_date"].ToString())) + "'");

                                            Insert_ProfessionalEducation.Append("),\n");

                                        }

                                    }

                                }


                                string query = Insert_Professional.ToString().Remove(Insert_Professional.Length - 2);

                                if (CheckQuery(query))
                                {
                                    Console.Write("Executing Professional: ");
                                    Insert_toDB(query, "");
                                }

                                query = Insert_ProfessionalEducation.ToString().Remove(Insert_ProfessionalEducation.Length - 2);
                                if (CheckQuery(query))
                                {
                                    Console.Write("Executing ProfessionalEducation: ");
                                    Insert_toDB(query, "");
                                }

                                query = Insert_ProfessionalExperience.ToString().Remove(Insert_ProfessionalExperience.Length - 2);
                                if (CheckQuery(query))
                                {
                                    Console.Write("Executing ProfessionalExperience: ");
                                    Insert_toDB(query, "");
                                }

                                query = Insert_ProfessionalLicense.ToString().Remove(Insert_ProfessionalLicense.Length - 2);
                                if (CheckQuery(query))
                                {
                                    Console.Write("Executing ProfessionalLicense: ");
                                    Insert_toDB(query, "");
                                }

                                query = Insert_ProfessionalLicenseStatusHistory.ToString().Remove(Insert_ProfessionalLicenseStatusHistory.Length - 2);
                                if (CheckQuery(query))
                                {
                                    Console.Write("Executing ProfessionalStatusHistory: ");
                                    Insert_toDB(query, "");
                                }

                                query = Insert_ProfessionalPositions.ToString().Remove(Insert_ProfessionalPositions.Length - 2);
                                if (CheckQuery(query))
                                {
                                    Console.Write("Executing ProfessionalPostions: ");
                                    Insert_toDB(query, "");
                                }

                                query = Insert_ProfessionalStatusHistory.ToString().Remove(Insert_ProfessionalStatusHistory.Length - 2);
                                if (CheckQuery(query))
                                {
                                    Console.Write("Executing ProfessionalStatusHistory: ");
                                    Insert_toDB(query, "");
                                }

                                query = Insert_ProfessionalFacilityGroup.ToString().Remove(Insert_ProfessionalFacilityGroup.Length - 2);
                                if (CheckQuery(query))
                                {
                                    Console.Write("Executing Insert_ProfessionalFacilityGroup: ");
                                    Insert_toDB(query, "");
                                }

                            }
                    }
                    else
                    {
                        result = "Content Empty";
                    }



                return "";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return null;
            }
        }
        public void InsertToDBfromRAW()
        {
            string directory = @"C:\Users\faisal\Desktop\files\DHA-Sheryan\Sheryan\All Result\Raw";
            string[] files = Directory.GetFiles(directory, "*.json");
            int count = 0;
            foreach (string file in files)
            {
                count++;
                string data = File.ReadAllText(file);
                Console.WriteLine("Working on file: " + count);
                if (ParseToInsert_Professional(data) == null)
                {
                    File.Move(file, directory + "\\Error\\" + Path.GetFileName(file));
                }
                else
                {
                    File.Move(file, directory + "\\Passed\\" + Path.GetFileName(file));

                }
            }
        }
        #endregion

        #region Facility
        public void GetFacility(string token)
        {
            string result = string.Empty;
            string URL = "https://api.qa.dubai.gov.ae/secure/dha/sheryan/getfacility/1.0.0/level3";
            string data = File.ReadAllText(baseDir + @"\GetFacility\Input_location.json");
            token = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
            try
            {

                int j = 100;
                int count = 0;
                for (int i = 1; i < 6000; i++)
                {
                    data = "{\r\n  \"StartIndex\": \"" + i + "\",\r\n  \"EndIndex\" :\"" + j + "\"\r\n}";
                    count++;

                    result = PostCall_ByBody(URL, data, token);
                    JObject json = JObject.Parse(result);
                    using (StreamWriter writer = File.CreateText(baseDir + @"\Sheryan_GF_" + System.DateTime.Now.ToString("yyyMMddHHmmss") + ".json"))
                    {
                        writer.Write(json.ToString());
                    }
                    Parse_Facility(json.ToString());

                    Console.WriteLine("Iteration Number: " + count);

                    i = i + 99;
                    j = i + 100;

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occured: " + ex.Message);
                Console.Read();
            }
        }
        public string Parse_Facility(string data)
        {
            string result = string.Empty;
            try
            {
                if (data != null)
                    if (data.Length > 0)
                    {
                        JObject yo = JObject.Parse(data);
                        StringBuilder SB = new StringBuilder();
                        if (yo["ReturnCode"] != null)
                            if (yo["ReturnCode"].ToString() == "00")
                            {
                                JArray Facility = (JArray)yo["Facilities"];

                                StringBuilder Insert_Facility = new StringBuilder();
                                Insert_Facility.Append("INSERT INTO [dbo].[GF_Facility]([facility_license_number],[facility_license_number_old_sheryan],[facility_license_number_mps],[facility_license_initial_issued_date],[facility_license_last_renewed_date],[facility_license_expiry_date],[facility_license_status_code],[facility_license_status_desc_en],[facility_license_status_desc_ar],[is_operation_allowed],[trade_license_authority_code],[trade_license_authority_en],[trade_license_authority_ar],[trade_license_number],[trade_license_expiry_date],[facility_has_pending_fines],[facility_name_en],[facility_name_ar],[facility_sector_code],[facility_sector_desc_en],[facility_sector_desc_ar],[facility_category_code],[facility_category_desc_en],[facility_category_desc_ar],[facility_group_identifier],[medical_director_license_number],[medical_director_first_name_en],[medical_director_first_name_ar],[medical_director_last_name_en],[medical_director_last_name_ar],[medical_director_primary_category_code],[medical_director_primary_category_desc_en],[medical_director_primary_category_desc_ar],[medical_director_primary_title_code],[medical_director_primary_title_desc_en],[medical_director_primary_title_desc_ar],[medical_director_primary_speciality_code],[medical_director_primary_speciality_en],[medical_director_primary_speciality_ar],[address_coordinate_x],[address_coordinate_y],[address_location_spatial],[address_makani_number_json],[address_street_name],[address_building_name],[address_apartment_villa_number],[address_area_code],[address_area_desc_ar],[address_area_desc_en],[address_city_code],[address_city_desc_ar],[address_city_desc_en],[address_emirate_code],[address_emirate_desc_en],[address_emirate_desc_ar],[contact_website],[contact_email],[contact_telephone],[is_premium_active],[premium_about],[premium_year_founded],[premium_Headquarters],[premium_size],[premium_operating_hours_json],[premium_cover_page_file_id],[premium_logo_file_id],[premium_insurance_companies_json],[last_modified_date]) VALUES ");

                                StringBuilder Insert_FacilitySpeciality = new StringBuilder();
                                Insert_FacilitySpeciality.Append("INSERT INTO [dbo].[GF_FacilitySpeciality]([facility_license_number],[facility_speciality_code],[facility_speciality_en],[facility_speciality_ar],[is_operation_allowed],[last_modified_date]) VALUES ");

                                StringBuilder Insert_FacilityAddon = new StringBuilder();
                                Insert_FacilityAddon.Append("INSERT INTO [dbo].[GF_FacilityAddon]([facility_license_number],[addon_type_code],[addon_type_desc_ar],[addon_type_desc_en],[start_date],[expiry_date],[last_modified_date]) VALUES ");

                                StringBuilder Insert_FacilityOpenFine = new StringBuilder();
                                Insert_FacilityOpenFine.Append("INSERT INTO [dbo].[GF_FacilityOpenFine]([facility_license_number],[violation_type_code],[violation_type_desc_en],[violation_type_desc_ar],[violation_against_code],[violation_against_desc_en],[violation_against_desc_ar],[professional_license_number],[violation_title],[violation_description_en],[violation_description_ar],[violation_date],[violation_source_code],[violation_source_desc_en],[violation_source_desc_ar],[violation_outcome_code],[violation_outcome_desc_en],[violation_outcome_desc_ar],[fine_amount],[violation_status_code],[violation_status_desc_en],[violation_status_desc_ar],[last_modified_date]) VALUES ");

                                StringBuilder Insert_FacilityLicenseStatusHistory = new StringBuilder();
                                Insert_FacilityLicenseStatusHistory.Append("INSERT INTO [dbo].[GF_FacilityLicenseStatusHistory]([facility_license_number],[license_status_code],[license_status_desc_en],[license_status_desc_ar],[license_status_effective_date],[last_modified_date]) VALUES ");

                                StringBuilder Insert_FFSpecialityStatusHistory = new StringBuilder();
                                Insert_FFSpecialityStatusHistory.Append("INSERT INTO [dbo].[GF_FFSpecialityStatusHistory]([facility_license_number],[facility_speciality_code],[facility_speciality_en],[facility_speciality_ar],[facility_speciality_status_code],[facility_speciality_status_ar],[facility_speciality_status_en],[facility_speciality_status_effective_from_date],[facility_speciality_status_effective_to_date],[last_modified_date]) VALUES ");

                                StringBuilder Insert_FacilityGroup = new StringBuilder();
                                Insert_FacilityGroup.Append("INSERT INTO [dbo].[GF_FacilityGroup]([facility_license_number],[facility_group_identifier],[facility_name_en],[facility_name_ar],[last_modified_date]) VALUES ");

                                for (int i = 0; i < Facility.Count; i++)
                                {
                                    if (yo["Facilities"][i]["Facility"].HasValues)
                                    {
                                        JObject FFacility = JObject.Parse(yo["Facilities"][i]["Facility"].ToString());

                                        Insert_Facility.Append("\n(");

                                        Insert_Facility.Append("" + CheckComma(FFacility["facility_license_number"].ToString()) + ",");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["facility_license_number_old_sheryan"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["facility_license_number_mps"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(ConvertDate(FFacility["facility_license_initial_issued_date"].ToString())) + "',");
                                        Insert_Facility.Append("'" + CheckComma(ConvertDate(FFacility["facility_license_last_renewed_date"].ToString())) + "',");
                                        Insert_Facility.Append("'" + CheckComma(ConvertDate(FFacility["facility_license_expiry_date"].ToString())) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["facility_license_status_code"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["facility_license_status_desc_en"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["facility_license_status_desc_ar"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["is_operation_allowed"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["trade_license_authority_code"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["trade_license_authority_en"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["trade_license_authority_ar"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["trade_license_number"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(ConvertDate(FFacility["trade_license_expiry_date"].ToString())) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["facility_has_pending_fines"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["facility_name_en"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["facility_name_ar"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["facility_sector_code"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["facility_sector_desc_en"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["facility_sector_desc_ar"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["facility_category_code"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["facility_category_desc_en"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["facility_category_desc_ar"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["facility_group_identifier"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["medical_director_license_number"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["medical_director_first_name_en"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["medical_director_first_name_ar"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["medical_director_last_name_en"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["medical_director_last_name_ar"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["medical_director_primary_category_code"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["medical_director_primary_category_desc_en"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["medical_director_primary_category_desc_ar"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["medical_director_primary_title_code"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["medical_director_primary_title_desc_en"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["medical_director_primary_title_desc_ar"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["medical_director_primary_speciality_code"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["medical_director_primary_speciality_en"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["medical_director_primary_speciality_ar"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["address_coordinate_x"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["address_coordinate_y"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["address_location_spatial"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["address_makani_number_json"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["address_street_name"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["address_building_name"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["address_apartment_villa_number"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["address_area_code"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["address_area_desc_ar"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["address_area_desc_en"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["address_city_code"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["address_city_desc_ar"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["address_city_desc_en"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["address_emirate_code"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["address_emirate_desc_en"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["address_emirate_desc_ar"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["contact_website"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["contact_email"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["contact_telephone"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["is_premium_active"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["premium_about"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["premium_year_founded"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["premium_Headquarters"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["premium_size"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["premium_operating_hours_json"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["premium_cover_page_file_id"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["premium_logo_file_id"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(FFacility["premium_insurance_companies_json"].ToString()) + "',");
                                        Insert_Facility.Append("'" + CheckComma(ConvertDate(FFacility["last_modified_date"].ToString())) + "'");

                                        Insert_Facility.Append("),\n");
                                    }

                                    if (yo["Facilities"][i]["FacilitySpeciality"].HasValues)
                                    {
                                        JArray FFSpeciality_Array = (JArray)yo["Facilities"][i]["FacilitySpeciality"];
                                        foreach (JObject FFSpeciality in FFSpeciality_Array)
                                        {
                                            Insert_FacilitySpeciality.Append("\n(");

                                            Insert_FacilitySpeciality.Append("" + CheckComma(FFSpeciality["facility_license_number"].ToString()) + ",");
                                            Insert_FacilitySpeciality.Append("'" + CheckComma(FFSpeciality["facility_speciality_code"].ToString()) + "',");
                                            Insert_FacilitySpeciality.Append("'" + CheckComma(FFSpeciality["facility_speciality_en"].ToString()) + "',");
                                            Insert_FacilitySpeciality.Append("'" + CheckComma(FFSpeciality["facility_speciality_ar"].ToString()) + "',");
                                            Insert_FacilitySpeciality.Append("'" + CheckComma(FFSpeciality["is_operation_allowed"].ToString()) + "',");
                                            Insert_FacilitySpeciality.Append("'" + CheckComma(ConvertDate(FFSpeciality["last_modified_date"].ToString())) + "'");

                                            Insert_FacilitySpeciality.Append("),\n");
                                        }
                                    }

                                    if (yo["Facilities"][i]["FacilitySpecialityStatuHistory"].HasValues)
                                    {
                                        JArray FFSpecialityStatusHistory_Array = (JArray)yo["Facilities"][i]["FacilitySpecialityStatuHistory"];
                                        foreach (JObject FFSpecialityStatusHistory in FFSpecialityStatusHistory_Array)
                                        {
                                            Insert_FFSpecialityStatusHistory.Append("\n(");

                                            Insert_FFSpecialityStatusHistory.Append("" + CheckComma(FFSpecialityStatusHistory["facility_license_number"].ToString()) + ",");
                                            Insert_FFSpecialityStatusHistory.Append("'" + CheckComma(FFSpecialityStatusHistory["facility_speciality_code"].ToString()) + "',");
                                            Insert_FFSpecialityStatusHistory.Append("'" + CheckComma(FFSpecialityStatusHistory["facility_speciality_en"].ToString()) + "',");
                                            Insert_FFSpecialityStatusHistory.Append("'" + CheckComma(FFSpecialityStatusHistory["facility_speciality_ar"].ToString()) + "',");
                                            Insert_FFSpecialityStatusHistory.Append("'" + CheckComma(FFSpecialityStatusHistory["facility_speciality_status_code"].ToString()) + "',");
                                            Insert_FFSpecialityStatusHistory.Append("'" + CheckComma(FFSpecialityStatusHistory["facility_speciality_status_ar"].ToString()) + "',");
                                            Insert_FFSpecialityStatusHistory.Append("'" + CheckComma(FFSpecialityStatusHistory["facility_speciality_status_en"].ToString()) + "',");
                                            Insert_FFSpecialityStatusHistory.Append("'" + CheckComma(ConvertDate(FFSpecialityStatusHistory["facility_speciality_status_effective_from_date"].ToString())) + "',");
                                            Insert_FFSpecialityStatusHistory.Append("'" + CheckComma(ConvertDate(FFSpecialityStatusHistory["facility_speciality_status_effective_to_date"].ToString())) + "',");
                                            Insert_FFSpecialityStatusHistory.Append("'" + CheckComma(ConvertDate(FFSpecialityStatusHistory["last_modified_date"].ToString())) + "'");

                                            Insert_FFSpecialityStatusHistory.Append("),\n");
                                        }
                                    }

                                    if (yo["Facilities"][i]["FacilityGroup"].HasValues)
                                    {
                                        JArray FFGroup_Array = (JArray)yo["Facilities"][i]["FacilityGroup"];
                                        foreach (JObject FFGroup in FFGroup_Array)
                                        {
                                            Insert_FacilityGroup.Append("\n(");

                                            Insert_FacilityGroup.Append("" + CheckComma(FFGroup["facility_license_number"].ToString()) + ",");
                                            Insert_FacilityGroup.Append("'" + CheckComma(FFGroup["facility_group_identifier"].ToString()) + "',");
                                            Insert_FacilityGroup.Append("'" + CheckComma(FFGroup["facility_name_en"].ToString()) + "',");
                                            Insert_FacilityGroup.Append("'" + CheckComma(FFGroup["facility_name_ar"].ToString()) + "',");
                                            Insert_FacilityGroup.Append("'" + CheckComma(FFGroup["last_modified_date"].ToString()) + "'");

                                            Insert_FacilityGroup.Append("),\n");
                                        }
                                    }

                                    if (yo["Facilities"][i]["FacilityAddon"].HasValues)
                                    {
                                        JArray FFAddon_Array = (JArray)yo["Facilities"][i]["FacilityAddon"];
                                        foreach (JObject FFAddon in FFAddon_Array)
                                        {
                                            Insert_FacilityAddon.Append("\n(");

                                            Insert_FacilityAddon.Append("" + CheckComma(FFAddon["facility_license_number"].ToString()) + ",");
                                            Insert_FacilityAddon.Append("'" + CheckComma(FFAddon["addon_type_code"].ToString()) + "',");
                                            Insert_FacilityAddon.Append("'" + CheckComma(FFAddon["addon_type_desc_ar"].ToString()) + "',");
                                            Insert_FacilityAddon.Append("'" + CheckComma(FFAddon["addon_type_desc_en"].ToString()) + "',");
                                            Insert_FacilityAddon.Append("'" + CheckComma(ConvertDate(FFAddon["start_date"].ToString())) + "',");
                                            Insert_FacilityAddon.Append("'" + CheckComma(ConvertDate(FFAddon["expiry_date"].ToString())) + "',");
                                            Insert_FacilityAddon.Append("'" + CheckComma(ConvertDate(FFAddon["last_modified_date"].ToString())) + "'");

                                            Insert_FacilityAddon.Append("),\n");
                                        }
                                    }

                                    if (yo["Facilities"][i]["FacilityOpenFine"].HasValues)
                                    {
                                        JArray FFOpenFines_Array = (JArray)yo["Facilities"][i]["FacilityOpenFine"];
                                        foreach (JObject FFOpenFines in FFOpenFines_Array)
                                        {
                                            Insert_FacilityOpenFine.Append("\n(");

                                            Insert_FacilityOpenFine.Append("" + CheckComma(FFOpenFines["facility_license_number"].ToString()) + ",");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_type_code"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_type_desc_en"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_type_desc_ar"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_against_code"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_against_desc_en"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_against_desc_ar"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["professional_license_number"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_title"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_description_en"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_description_ar"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(ConvertDate(FFOpenFines["violation_date"].ToString())) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_source_code"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_source_desc_en"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_source_desc_ar"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_outcome_code"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_outcome_desc_en"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_outcome_desc_ar"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["fine_amount"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_status_code"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_status_desc_en"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(FFOpenFines["violation_status_desc_ar"].ToString()) + "',");
                                            Insert_FacilityOpenFine.Append("'" + CheckComma(ConvertDate(FFOpenFines["last_modified_date"].ToString())) + "'");

                                            Insert_FacilityOpenFine.Append("),\n");

                                        }
                                    }

                                    if (yo["Facilities"][i]["FacilityLicenseStatusHistory"].HasValues)
                                    {
                                        JArray FFLicenseStatusHistory_Array = (JArray)yo["Facilities"][i]["FacilityLicenseStatusHistory"];
                                        foreach (JObject FFLicenseStatusHistory in FFLicenseStatusHistory_Array)
                                        {
                                            Insert_FacilityLicenseStatusHistory.Append("\n(");

                                            Insert_FacilityLicenseStatusHistory.Append("" + CheckComma(FFLicenseStatusHistory["facility_license_number"].ToString()) + ",");
                                            Insert_FacilityLicenseStatusHistory.Append("'" + CheckComma(FFLicenseStatusHistory["license_status_code"].ToString()) + "',");
                                            Insert_FacilityLicenseStatusHistory.Append("'" + CheckComma(FFLicenseStatusHistory["license_status_desc_en"].ToString()) + "',");
                                            Insert_FacilityLicenseStatusHistory.Append("'" + CheckComma(FFLicenseStatusHistory["license_status_desc_ar"].ToString()) + "',");
                                            Insert_FacilityLicenseStatusHistory.Append("'" + CheckComma(ConvertDate(FFLicenseStatusHistory["license_status_effective_date"].ToString())) + "',");
                                            Insert_FacilityLicenseStatusHistory.Append("'" + CheckComma(ConvertDate(FFLicenseStatusHistory["last_modified_date"].ToString())) + "'");

                                            Insert_FacilityLicenseStatusHistory.Append("),\n");
                                        }
                                    }

                                }

                                Console.Clear();

                                string query = Insert_Facility.ToString().Remove(Insert_Facility.Length - 2);

                                if (CheckQuery(query))
                                {
                                    Console.Write("Executing Facility: ");
                                    Insert_toDB(query, "");
                                }

                                query = Insert_FacilityAddon.ToString().Remove(Insert_FacilityAddon.Length - 2);
                                if (CheckQuery(query))
                                {
                                    Console.Write("Executing FacilityAddon: ");
                                    Insert_toDB(query, "");
                                }

                                query = Insert_FacilityLicenseStatusHistory.ToString().Remove(Insert_FacilityLicenseStatusHistory.Length - 2);
                                if (CheckQuery(query))
                                {
                                    Console.Write("Executing FacilityLicenseStatusHistory: ");
                                    Insert_toDB(query, "");
                                }

                                query = Insert_FacilityOpenFine.ToString().Remove(Insert_FacilityOpenFine.Length - 2);
                                if (CheckQuery(query))
                                {
                                    Console.Write("Executing FacilityOpenFine: ");
                                    Insert_toDB(query, "");
                                }

                                query = Insert_FacilitySpeciality.ToString().Remove(Insert_FacilitySpeciality.Length - 2);
                                if (CheckQuery(query))
                                {
                                    Console.Write("Executing FacilitySpeciality: ");
                                    Insert_toDB(query, "");
                                }

                                query = Insert_FFSpecialityStatusHistory.ToString().Remove(Insert_FFSpecialityStatusHistory.Length - 2);
                                if (CheckQuery(query))
                                {
                                    Console.Write("Executing FFSpecialityStatusHistory: ");
                                    Insert_toDB(query, "");
                                }

                                query = Insert_FacilityGroup.ToString().Remove(Insert_FacilityGroup.Length - 2);
                                if (CheckQuery(query))
                                {
                                    Console.Write("Executing FacilityGroup: ");
                                    Insert_toDB(query, "");
                                }
                            }
                    }
                return result;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        #endregion

        #region CustomFunction

        public int GetMAXList(List<LicenseActive> list_license)
        {
            int chosen = 1;
            DateTime date = new DateTime();

            foreach (var obj in list_license)
            {
                if (date < Convert.ToDateTime(obj.ActiveTo))
                {
                    date = Convert.ToDateTime(obj.ActiveTo);
                    chosen = obj.counter;
                }
            }

            return chosen;
        }
        public string GetMaxExpiryDate(List<string> list)
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
        public string LimitString(string data, int size)
        {

            try
            {
                if (data.Length > size)
                    data = data.Substring(0, size);
            }
            catch (Exception ex)
            {
                CustomLog.Info("cannot limit this " + data);
            }
            return data;
        }
        public string CheckGender(string data)
        {
            string result = string.Empty;
            try
            {
                if (data != null)
                {
                    if (data.ToLower() == "male")
                    {
                        result = "Male";
                    }
                    else if (data.ToLower() == "female")
                    {
                        result = "Female";
                    }
                    else
                    {
                        result = "";
                    }

                }

            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                CustomLog.Info(ex.Message);
            }

            return result;
        }
        public string PostCall_ByBody(string URL, string postdata, string staticToken)
        {
            string result = string.Empty;
            bool OKAY = false;
            int counter = 0;
            int counter_limit = int.Parse(ConfigurationManager.AppSettings.Get("counter_limit"));
            try
            {
                while (!OKAY)
                {
                    try
                    {
                        using (WebClient wc = new WebClient())
                        {
                            wc.Headers[HttpRequestHeader.ContentType] = "application/json";
                            wc.Headers[HttpRequestHeader.Authorization] = staticToken;
                            CustomLog.Info("Sheryan called at " + DateTime.Now);
                            result = wc.UploadString(URL, postdata);
                            //CustomLog.Info(result);
                            if (result != null)
                                if (result.Length > 0)
                                {
                                    JObject yo = JObject.Parse(result);
                                    if (yo["ReturnCode"] != null)
                                    {
                                        Console.WriteLine("Return Code: " + yo["ReturnCode"].ToString());
                                        if (yo["ReturnCode"].ToString() == "00" || yo["ReturnCode"].ToString() == "10" || yo["ReturnCode"].ToString() == "07")
                                        {
                                            OKAY = true;
                                            //("Results found for data: " + postdata);
                                            //CustomLog.Info("Results found for data: " + postdata);
                                        }
                                    }
                                    else
                                    {
                                        Console.WriteLine("Result: " + yo.ToString());
                                        CustomLog.Info(yo.ToString());
                                    }
                                }
                                else if (counter > counter_limit)
                                {
                                    OKAY = true;
                                    CustomLog.Info("Tried hitting the service " + counter_limit + " times but no results found");
                                }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Write(ex.Message);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Occured in PostCall_ByBody !\n" + result);
                return ex.Message;
            }
        }
        public void Insert_toDB(string query, string connection)
        {
            connection = "Data Source=10.162.176.206 ;Initial Catalog=FS_WS_WSCTFW ;User ID=fshaikh ;Password=Dell@900 ;Connection Timeout=30;";

            try
            {
                using (SqlConnection con = new SqlConnection(connection))
                {
                    using (SqlCommand command = new SqlCommand(query, con))
                    {
                        con.Open();
                        if (command.ExecuteNonQuery() > 0)
                        {
                            Console.WriteLine("Query executed successfully");
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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
                    if (result.Contains("\""))
                    {
                        result = result.Replace("\"", "");
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
        private static bool CheckQuery(string data)
        {
            bool result = false;
            try
            {
                if (data.EndsWith(")"))
                {
                    result = true;
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception CheckQuery: " + ex.Message);
                return false;
            }
        }
        public static string CheckNull(string data)
        {
            string result = string.Empty;
            if (data == null || data == "\n" || data == string.Empty || data == "\\n      " || data == "--")
            {
                result = string.Empty;
            }
            else
            {
                result = data;
            }
            return result;
        }
        public static string CheckActive(string data)
        {
            string active = string.Empty;

            if (data.ToString() == "0")
                active = "Deactivated";
            if (data.ToString() == "1")
                active = "Active";

            //if (data.ToString() == "0")
            //    active = "FALSE";
            //if (data.ToString() == "1")
            //    active = "TRUE";


            return active;
        }
        public static string ApplyClinicianFormula(string data)
        {
            if (data != "NULL" && data != "\\n" && data != string.Empty)
            {
                if (data[0].ToString() == "0")
                {
                    data = "DHA-P-" + data.Substring(1);
                }
                else
                {
                    data = "DHA-P-" + data;
                }
            }
            return data;
        }
        public static string ApplyReverseClinicianFormula(string data)
        {
            if (data != "NULL" && data != "\\n" && data != string.Empty)
            {
                data.Replace("DHA-P-", "");
                if(data.Length == 7)
                {
                    data = "0" + data;
                }
            }
            return data;
        }
        public static string ApplyFacilityFormula(string data)
        {
            //if (data[0].ToString() == "0")
            //{
            //    data = "DHA-F-" + data.Substring(1);
            //}
            //else
            //{
            //    data = "DHA-F-" + data;
            //}
            if (data != "NULL" && data != "\\n" && data != string.Empty)
            {
                data = "DHA-F-" + data;
            }


            return data;
        }
        public static string ListToCommaSeperated(List<string> list)
        {
            string result = string.Empty;
            if (list.Count > 0)
            {
                foreach (string data in list)
                {
                    result += data + "$";
                }
            }
            result = result.Remove(result.Length - 1, 1);
            return result;
        }
        public static void RemoveDuplicates(string filepath)
        {
            CustomLog.Info("Removing Duplicates");
            try
            {
                string[] linese = File.ReadAllLines(filepath);
                File.WriteAllLines(filepath, linese.Distinct().ToArray());
            }
            catch (Exception ex)
            {
                CustomLog.Info(ex.Message + ex.InnerException);
            }
            CustomLog.Info("Duplicates Removed");

        }
        #endregion

        #region Structs

        public class GateParams
        {
            public string DHA_Input;

            public string LMU_isActive;
            public string LMU_Source;
            public string LMU_Status;

            public string Out_isActive;
            public string Out_Source;

        }

        public struct LicenseActive
        {
            public int counter { get; set; }
            public string ActiveFrom { get; set; }
            public string ActiveTo { get; set; }
            public string SpecialityDesc { get; set; }
        }

        public struct LMU_Professional
        {
            public string ClinicianLicene;
            public string ClinicianIdOld;
            public string ClinicianName;
            public string Username;
            public string Password;
            public string FacilityLicense;
            public string FacilityName;
            public string Location;
            public string ActiveFrom;
            public string ActiveTo;
            public string Active;
            public string Source;
            public string SpecialityId1;
            public string SpecialityDescription;
            public string Gender;
            public string Nationality;
            public string Email;
            public string Phone;
            public string SpecialityId2;
            public string SpecialityId3;
            public string Type;
        }
        public struct Master_Clinician
        {
            public string ClinicianLicense;
            public string ClinicianName;
            public string Active;
            public string SpecialityFieldId;
            public string Nationality;
            public string Phone;
            public string Email;
            public string Gender;
            public string OldUniqueId;
            public string Type;
            public string Source;
        }

        public struct SheryanPublicList
        {
            public string License_derived_eClaims_ID;
            public string UniqueID;
            public string Professional_is_practice_allowed;
            public string Professional_name;
            public string License_type;
            public string License_status;
            public string License_initial_issued_date;
            public string License_last_renewed_date;
            public string License_effective_date;
            public string License_expiry_date;
            public string Facility_license_number;
            public string Primary_facility_name;
            public string Facility_city;
            public string eClaim_specialty_code;
            public string Nationality;
            public string Work_phone;
            public string Work_email;
            public string Gender;
            public string History_status_latest_record;
            public string History_status_effective_from_date;
            public string History_status_effective_to_date;
            public string Type;
            public string Source;
            public string Facility_group_identifier;
        }


        public struct SPL_Professional
        {
            public string UniqueID;
            public string Professional_name;
            public string Professional_is_practice_allowed;
            public string Nationality;
            public string Work_phone;
            public string Work_email;
            public string Gender;
            public string Type;
            public string Source;
            public string last_modified_date;

        }
        public struct SPL_Positions
        {
            public string unique_id;
            //public string professional_license_number;
            //public string primarySpeciality;
            public string eclaim_code;
            //public string is_primary;
            public string last_modified_date;

        }
        public struct SPL_StatusHistory
        {
            public string unique_id;
            public string History_status_latest_record;
            public string History_status_effective_from_date;
            public string History_status_effective_to_date;
            public string last_modified_date;
        }
        public struct SPL_License
        {
            public string unique_id;
            public string License_derived_eClaims_ID;
            //public string professional_license_number_old;
            public string License_type;
            public string License_status;
            public string License_initial_issued_date;
            public string License_last_renewed_date;
            public string License_expiry_date;
            public string Facility_license_number;
            public string last_modified_date;
        }
        public struct SPL_LicenseStatusHistory
        {
            public string License_effective_date;
            //public string professional_license_number;
            //public string license_status_code;
            //public string license_status_desc_en;
            public string last_modified_date;

        }
        public struct SPL_FacilityGroup
        {
            public string unique_id;
            public string Primary_facility_name;
            public string Facility_group_identifier;
            public string last_modified_date;

        }
        public struct SPL_Experience
        {
            public string unique_id;
            //public string professional_license_number;
            //public string professional_license_number_old;
            //public string license_type_code;
            //public string license_status_desc_en;
            public string work_location_city_desc_en;
            public string last_modified_date;
        }



        #endregion
    }


}

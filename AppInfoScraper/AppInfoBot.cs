using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using CrawlerLib.Net;
using System.Collections.Specialized;
using System.Data.OleDb;

namespace AppInfoScraper
{
    class AppInfoBot
    {
        public IAppInfoBot main;
        WebclientX client = new WebclientX();
        public string store = "";
        public string region = "";
        public string category = "";
        public bool stop = true;
        string proxy_file = "proxy.txt";
        string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=app_review.accdb;Persist Security Info=False;";
        OleDbConnection myDataConnection;
        OleDbCommand nonqueryCommand;

        public void runAppStore()
        {
            client = new WebclientX();
            myDataConnection = new OleDbConnection(connectionString);
            myDataConnection.Open();


            nonqueryCommand = myDataConnection.CreateCommand();
            nonqueryCommand.CommandText = "DELETE FROM review WHERE review.appid IN (SELECT app.appid FROM app WHERE store='appstore')";
            nonqueryCommand.ExecuteNonQuery();
            nonqueryCommand.Dispose();

            nonqueryCommand = myDataConnection.CreateCommand();
            nonqueryCommand.CommandText = "DELETE FROM app WHERE store='appstore'";
            nonqueryCommand.ExecuteNonQuery();
            nonqueryCommand.Dispose();

            stop = false;
            string region_code = "";
            if (region == "US")
            {
                region_code = "us/";
                region = "";
            }
            else if (region == "HongKong")
            {
                region_code = "hk/";
            }
            else if (region == "UK")
            {
                region_code = "gb/";
            }

            string letters = "QWERTYUIOPASDFGHJKLZXCVBNM#";
            JObject json;

            for (int i = 0; i < letters.Length; i++)
            {
                if (stop == true)
                    break;
                string l = letters.Substring(i, 1);
                int page = 1;
                while (true)
                {
                    if (stop == true)
                        break;
                    List<string> app_links = new List<string>();
                    List<string> app_ids = new List<string>();
                    string content = "";
                    content = Utility.ReadFileString("as_app_dir/letter_" + region+l + "_" + page + ".txt");
                    if (content == "")
                    {
                        content = client.GetMethod("https://itunes.apple.com/" + region_code + "genre/ios-travel/id6003?mt=8&letter=" + l + "&page=" + page);
                        Utility.WriteFile("as_app_dir/letter_" + region+l + "_" + page + ".txt", content, false);
                    }
                    page++;
                    app_links = Utility.SimpleRegex("https://itunes.apple.com/" + region_code + "app/[a-z\\-]*/id([0-9]{7,})\\?mt=8", content, 0, System.Text.RegularExpressions.RegexOptions.Singleline);
                    app_ids = Utility.SimpleRegex("https://itunes.apple.com/" + region_code + "app/[a-z\\-]*/id([0-9]{7,})\\?mt=8", content, 1);
                    main.Log("Found " + app_links.Count + " apps in page " + page + " at letter " + l);

                    for (int j = 0; j < app_links.Count; j++)
                    {
                        if (stop == true)
                            break;
                        try
                        {
                            string applink = app_links[j];
                            string appid = app_ids[j];
                            client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.97 Safari/537.11");
                            content = Utility.ReadFileString("as_app/" + region + appid + ".txt");
                            if (content == "")
                            {
                                content = client.GetMethod(applink);
                                Utility.WriteFile("as_app/" +region+ appid + ".txt", content, false);
                            }
                            client.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");
                            string title = Utility.SimpleRegexSingle("class=\"intro\\s*\">\\s*<div class=\"left\">\\s*<h1>([^<]*)", content, 1);
                            main.Log("Scraping app \"" + title + "\"");
                            string description = Utility.SimpleRegexSingle("<h4>\\s*Description\\s*</h4>\\s*<p>(.*?)(?=</p>)", content, 1);
                            description = description.Replace("|", " ");
                            description = description.Replace("\n", " ");
                            description = description.Replace("\r", " ");
                            string price = Utility.SimpleRegexSingle("class=\"price\">([^<]*)", content, 1);
                            string released_date = Utility.SimpleRegexSingle(">Released:\\s*</span>([^<]*)", content, 1);
                            string update_date = Utility.SimpleRegexSingle(">Updated:\\s*</span>([^<]*)", content, 1);
                            string developer = Utility.SimpleRegexSingle(">Developer:\\s*</span>([^<]*)", content, 1);
                            developer = developer.Replace("|", " ");
                            string seller = Utility.SimpleRegexSingle(">Seller:\\s*</span>([^<]*)", content, 1);
                            seller = seller.Replace("|", " ");
                            string version = Utility.SimpleRegexSingle(">Version:\\s*</span>([^<]*)", content, 1);

                            client.Headers.Add("User-Agent", "iTunes/11.0 (Windows; Microsoft Windows 7 x64 Ultimate Edition Service Pack 1 (Build 7601)) AppleWebKit/536.27.1");

                            content = Utility.ReadFileString("as_review/review_main_" + appid + ".txt");
                            if (content == "")
                            {
                                content = client.GetMethod("https://itunes.apple.com/" + region_code + "customer-reviews/id" + appid + "?dataOnly=true&displayable-kind=11&appVersion=all");
                                Utility.WriteFile("as_review/review_main_" + appid + ".txt", content, false);
                            }

                            json = JObject.Parse(content);
                            string rating_count = "0";
                            string average_rate = "0";
                            string rate1 = "0";
                            string rate2 = "0";
                            string rate3 = "0";
                            string rate4 = "0";
                            string rate5 = "0";
                            int totalNumberOfReviews = (int)json["totalNumberOfReviews"];
                            if (totalNumberOfReviews != 0)
                            {
                                rating_count = (string)json["ratingCount"];
                                average_rate = (string)json["ratingAverage"];
                                rate1 = (string)json["ratingCountList"][0];
                                rate2 = (string)json["ratingCountList"][1];
                                rate3 = (string)json["ratingCountList"][2];
                                rate4 = (string)json["ratingCountList"][3];
                                rate5 = (string)json["ratingCountList"][4];
                            }

                            nonqueryCommand = myDataConnection.CreateCommand();
                            nonqueryCommand.CommandText = "INSERT  INTO app (appid, title,description,price,released_date,update_date,developer,seller,version,rating_count,average_rate,rate1,rate2,rate3,rate4,rate5,store,region) VALUES (@appid, @title,@description,@price,@released_date,@update_date,@developer,@seller,@version,@rating_count,@average_rate,@rate1,@rate2,@rate3,@rate4,@rate5,'appstore',@region)";
                            nonqueryCommand.Parameters.AddWithValue("appid", appid);
                            nonqueryCommand.Parameters.AddWithValue("title", title);
                            nonqueryCommand.Parameters.AddWithValue("description", description);
                            nonqueryCommand.Parameters.AddWithValue("price", price);
                            nonqueryCommand.Parameters.AddWithValue("released_date", released_date);
                            nonqueryCommand.Parameters.AddWithValue("update_date", update_date);
                            nonqueryCommand.Parameters.AddWithValue("developer", developer);
                            nonqueryCommand.Parameters.AddWithValue("seller", seller);
                            nonqueryCommand.Parameters.AddWithValue("version", version);
                            nonqueryCommand.Parameters.AddWithValue("rating_count", rating_count);
                            nonqueryCommand.Parameters.AddWithValue("average_rate", average_rate);
                            nonqueryCommand.Parameters.AddWithValue("rate1", rate1);
                            nonqueryCommand.Parameters.AddWithValue("rate2", rate2);
                            nonqueryCommand.Parameters.AddWithValue("rate3", rate3);
                            nonqueryCommand.Parameters.AddWithValue("rate4", rate4);
                            nonqueryCommand.Parameters.AddWithValue("rate5", rate5);
                            nonqueryCommand.Parameters.AddWithValue("region", region);
                            nonqueryCommand.ExecuteNonQuery();
                            nonqueryCommand.Dispose();

                            int from = 0;
                            int to = 100;
                            int count_review = 1;
                            int review_count = 0;
                            while (true && totalNumberOfReviews > 0)
                            {
                                if (stop == true)
                                    break;
                                client.Headers.Add("User-Agent", "iTunes/11.0 (Windows; Microsoft Windows 7 x64 Ultimate Edition Service Pack 1 (Build 7601)) AppleWebKit/536.27.1");
                                content = Utility.ReadFileString("as_review/review_" + region+appid + "_" + count_review + ".txt");
                                if (content == "")
                                {
                                    content = client.GetMethod("https://itunes.apple.com/WebObjects/MZStore.woa/wa/userReviewsRow?id=" + appid + "&displayable-kind=11&startIndex=" + from + "&endIndex=" + to + "&sort=1"); //("https://itunes.apple.com/WebObjects/MZStore.woa/wa/userReviewsRow?id=581264644&displayable-kind=11&startIndex=0&endIndex=2&sort=1&appVersion=all");// 
                                    Utility.WriteFile("as_review/review_" + region+appid + "_" + count_review + ".txt", content, false);
                                }

                                count_review++;
                                json = JObject.Parse(content);
                                if (json["userReviewList"].Count() == 0)
                                    break;

                                int userReviewList = json["userReviewList"].Count();
                                main.Log("Found " + userReviewList + " reviews");

                                for (int k = 0; k < userReviewList; k++)
                                {
                                    if (stop == true)
                                        break;
                                    string rate_title = (string)json["userReviewList"][k]["title"];
                                    rate_title = rate_title.Replace("|", " ");
                                    string rate_rate = (string)json["userReviewList"][k]["rating"];
                                    string rate_by = (string)json["userReviewList"][k]["name"];
                                    rate_by = rate_by.Replace("|", " ");
                                    string rate_date = (string)json["userReviewList"][k]["date"];
                                    string rate_content = (string)json["userReviewList"][k]["body"];
                                    rate_content = rate_content.Replace("|", " ");
                                    rate_content = rate_content.Replace("\n", " ");
                                    rate_content = rate_content.Replace("\r", " ");
                                    string rate_vote_count = (string)json["userReviewList"][k]["voteCount"];
                                    string rate_vote_sum = (string)json["userReviewList"][k]["voteSum"];


                                    nonqueryCommand = myDataConnection.CreateCommand();
                                    nonqueryCommand.CommandText = "INSERT  INTO review (appid, app_title, title, rate,rate_by,rate_date,content,vote_count,vote_sum,version) VALUES (@appid, @app_title, @title, @rate,@rate_by,@rate_date,@content,@vote_count,@vote_sum,@version)";

                                    nonqueryCommand.Parameters.AddWithValue("@appid", appid);
                                    nonqueryCommand.Parameters.AddWithValue("@app_title", title);
                                    nonqueryCommand.Parameters.AddWithValue("@title", rate_title);
                                    nonqueryCommand.Parameters.AddWithValue("@rate", rate_rate);
                                    nonqueryCommand.Parameters.AddWithValue("@rate_by", rate_by);
                                    nonqueryCommand.Parameters.AddWithValue("@rate_date", rate_date);
                                    nonqueryCommand.Parameters.AddWithValue("@content", rate_content);
                                    nonqueryCommand.Parameters.AddWithValue("@vote_count", rate_vote_count);
                                    nonqueryCommand.Parameters.AddWithValue("@vote_sum", rate_vote_sum);
                                    nonqueryCommand.Parameters.AddWithValue("@version", "");
                                    nonqueryCommand.ExecuteNonQuery();
                                    nonqueryCommand.Dispose();
                                    review_count++;

                                }
                                from = to + 1;
                                to += 50;
                            }//loop comments
                            nonqueryCommand = myDataConnection.CreateCommand();
                            nonqueryCommand.CommandText = "UPDATE app SET review_count=@review_count WHERE appid=@appid";
                            nonqueryCommand.Parameters.AddWithValue("review_count", review_count);
                            nonqueryCommand.Parameters.AddWithValue("appid", appid);
                            nonqueryCommand.ExecuteNonQuery();
                            nonqueryCommand.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Utility.WriteLog(ex.Message);
                            Utility.WriteLog(ex.StackTrace);
                        }

                    }//for (int j = 0; j < app_links.Count; j++)
                    if (app_links.Count < 10)
                        break;
                }//loop pages
            }//loop letters

            myDataConnection.Close();
            myDataConnection.Dispose();

            stop = true;
            main.Finished();
        }

        public void runGooglePlay()
        {
            client = new WebclientX();

            myDataConnection = new OleDbConnection(connectionString);
            myDataConnection.Open();

            nonqueryCommand = myDataConnection.CreateCommand();
            nonqueryCommand.CommandText = "DELETE FROM review WHERE review.appid IN (SELECT app.appid FROM app WHERE store='googleplay')";
            nonqueryCommand.ExecuteNonQuery();
            nonqueryCommand.Dispose();

            nonqueryCommand = myDataConnection.CreateCommand();
            nonqueryCommand.CommandText = "DELETE FROM app WHERE store='googleplay'";
            nonqueryCommand.ExecuteNonQuery();
            nonqueryCommand.Dispose();

            stop = false;
            client.Headers.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; WOW64; Trident/5.0)");

            for (int i = 0; i <= 480; i += 24)
            {
                if (stop == true)
                    break;
                string content = "";
                while (content == "")
                {
                    client.SetProxyFile(proxy_file);
                    content = Utility.ReadFileString("gp_app_dir/paid_page_" + i + ".txt");
                    if (content == "")
                    {
                        content = client.GetMethod("https://play.google.com/store/apps/category/TRAVEL_AND_LOCAL/collection/topselling_paid?start=" + i + "&num=24");
                        Utility.WriteFile("gp_app_dir/paid_page_" + i + ".txt", content, false);
                    }

                }
                ParseGPItem(content);
                content = "";
                while (content == "")
                {
                    client.SetProxyFile(proxy_file);
                    content = Utility.ReadFileString("gp_app_dir/free_page_" + i + ".txt");
                    if (content == "")
                    {
                        content = client.GetMethod("https://play.google.com/store/apps/category/TRAVEL_AND_LOCAL/collection/topselling_free?start=" + i + "&num=24");
                        Utility.WriteFile("gp_app_dir/page_" + i + ".txt", content, false);
                    }
                }
                ParseGPItem(content);
            }


            myDataConnection.Close();
            myDataConnection.Dispose();

            stop = true;
            main.Finished();
        }

        void ParseGPItem(string content)
        {
            List<string> app_links = new List<string>();
            List<string> app_ids = new List<string>();
            app_ids = Utility.SimpleRegex("data-a=\"1\" data-c=\"1\" href=\"/store/apps/details\\?id=([^&]*)", content, 1);
            app_links = Utility.SimpleRegex("data-a=\"1\" data-c=\"1\" href=\"([^\"]*)\">", content, 1);
            for (int j = 0; j < app_links.Count; j++)
            {
                if (stop == true)
                    break;
                try
                {
                    string applink = "https://play.google.com" + app_links[j];
                    applink = applink.Replace("&amp;", "&");
                    string appid = app_ids[j];

                    content = "";
                    while (content == "")
                    {
                        client.SetProxyFile(proxy_file);
                        content = Utility.ReadFileString("gp_app/app_" + appid + ".txt");
                        if (content == "")
                        {
                            content = client.GetMethod(applink);
                            Utility.WriteFile("gp_app/app_" + appid + ".txt", content, false);
                        }
                    }

                    string title = Utility.SimpleRegexSingle("\"doc-banner-title\">([^<]*)<", content, 1);
                    main.Log("Scraping app \"" + title + "\"");
                    string description = Utility.SimpleRegexSingle("\"description\">(.*?)(?=</div>)", content, 1);
                    description = description.Replace("|", " ");
                    description = description.Replace("\n", " ");
                    description = description.Replace("\r", " ");
                    string price = Utility.SimpleRegexSingle("\"price\" content=\"([^\"]*)", content, 1);
                    string released_date = Utility.SimpleRegexSingle("", content, 1);
                    string update_date = Utility.SimpleRegexSingle("\"datePublished\">([^<]*)", content, 1);
                    string developer = Utility.SimpleRegexSingle("doc-header-link\">([^<]*)", content, 1);
                    developer = developer.Replace("|", " ");
                    string version = Utility.SimpleRegexSingle("softwareVersion\">([^<]*)", content, 1);
                    string rating_count = Utility.SimpleRegexSingle("ratingCount\" content=\"([^\"]*)\"", content, 1);
                    string average_rate = Utility.SimpleRegexSingle("ratingValue\" content=\"([^\"]*)\"", content, 1);
                    string rate1 = Utility.SimpleRegexSingle("bar bar1\".*?(?=<span>)<span>([^<]*)", content, 1).Replace(",", "");
                    string rate2 = Utility.SimpleRegexSingle("bar bar2\".*?(?=<span>)<span>([^<]*)", content, 1).Replace(",", "");
                    string rate3 = Utility.SimpleRegexSingle("bar bar3\".*?(?=<span>)<span>([^<]*)", content, 1).Replace(",", "");
                    string rate4 = Utility.SimpleRegexSingle("bar bar4\".*?(?=<span>)<span>([^<]*)", content, 1).Replace(",", "");
                    string rate5 = Utility.SimpleRegexSingle("bar bar5\".*?(?=<span>)<span>([^<]*)", content, 1).Replace(",", "");

                    nonqueryCommand = myDataConnection.CreateCommand();
                    nonqueryCommand.CommandText = "INSERT  INTO app (appid, title,description,price,released_date,update_date,developer,version,rating_count,average_rate,rate1,rate2,rate3,rate4,rate5,store) VALUES (@appid, @title,@description,@price,@released_date,@update_date,@developer,@version,@rating_count,@average_rate,@rate1,@rate2,@rate3,@rate4,@rate5,'googleplay')";
                    nonqueryCommand.Parameters.AddWithValue("appid", appid);
                    nonqueryCommand.Parameters.AddWithValue("title", title);
                    nonqueryCommand.Parameters.AddWithValue("description", description);
                    nonqueryCommand.Parameters.AddWithValue("price", price);
                    nonqueryCommand.Parameters.AddWithValue("released_date", released_date);
                    nonqueryCommand.Parameters.AddWithValue("update_date", update_date);
                    nonqueryCommand.Parameters.AddWithValue("developer", developer);
                    nonqueryCommand.Parameters.AddWithValue("version", version);
                    nonqueryCommand.Parameters.AddWithValue("rating_count", rating_count);
                    nonqueryCommand.Parameters.AddWithValue("average_rate", average_rate);
                    nonqueryCommand.Parameters.AddWithValue("rate1", rate1);
                    nonqueryCommand.Parameters.AddWithValue("rate2", rate2);
                    nonqueryCommand.Parameters.AddWithValue("rate3", rate3);
                    nonqueryCommand.Parameters.AddWithValue("rate4", rate4);
                    nonqueryCommand.Parameters.AddWithValue("rate5", rate5);
                    nonqueryCommand.ExecuteNonQuery();
                    nonqueryCommand.Dispose();

                    string token = Utility.SimpleRegexSingle(", token: '([^']*)',", content, 1);
                    int total_pages = 0;
                    int current_page = 1;
                    NameValueCollection prms = new NameValueCollection();
                    int review_count = 0;
                    do
                    {
                        if (stop == true)
                            break;
                        prms = new NameValueCollection();
                        prms.Add("xhr", "1");
                        prms.Add("token", token);
                        content = "";
                        content = Utility.ReadFileString("gp_app_review/review_" + appid + "_" + current_page + ".txt");

                        if (content == "")
                        {
                            content = client.PostMethod("https://play.google.com/store/getreviews?id=" + appid + "&reviewSortOrder=2&reviewType=1&pageNum=" + current_page, prms);
                            Utility.WriteFile("gp_app_review/review_" + appid + "_" + current_page + ".txt", content, false);
                        }
                        if (content == "")
                            break;
                        current_page++;
                        if (total_pages == 0)
                        {
                            total_pages = int.Parse(Utility.SimpleRegexSingle("\"numPages\":(\\d+)", content, 1));
                            main.Log("Found " + (total_pages * 10) + " reviews");
                        }
                        content = Utility.SimpleRegexSingle("\"htmlContent\":\"(.*?)(?=\",\"numPages\":)", content, 1);
                        content = Utility.HtmlDecode(content).Replace("\\u003C", "<");
                        content = Utility.HtmlDecode(content).Replace("\\\"", "\"");
                        content = Utility.HtmlDecode(content).Replace("\\/", "/");

                        List<string> comment_blocks = Utility.SimpleRegex("doc-review\">.*?(?=<hr>)", content, 0);

                        foreach (string cb in comment_blocks)
                        {
                            if (stop == true)
                                break;
                            string rate_title = Utility.SimpleRegexSingle("review-title\">([^<]*)", cb, 1);
                            rate_title = rate_title.Replace("|", " ");
                            string rate_rate = Utility.SimpleRegexSingle("Rating: ([0-9\\.]*) stars", cb, 1);
                            string rate_by = Utility.SimpleRegexSingle("doc-review-author\"><strong>([^<]*)", cb, 1);
                            if (rate_by == "")
                                rate_by = Utility.SimpleRegexSingle("target=\"_blank\"><strong>([^<]*)", cb, 1);
                            rate_by = rate_by.Replace("|", " ");
                            string rate_date = Utility.SimpleRegexSingle("doc-review-date\"> - ([^<]*)", cb, 1);
                            string rate_content = Utility.SimpleRegexSingle("eview-text\">([^<]*)", cb, 1);
                            rate_content = rate_content.Replace("|", " ");
                            rate_content = rate_content.Replace("\n", " ");
                            rate_content = rate_content.Replace("\r", " ");
                            string rate_version = Utility.SimpleRegexSingle("[vV]ersion ([0-9\\.]*)", cb, 1);
                            string device = Utility.SimpleRegexSingle("</span>\\s*-\\s*(.*?)(?=with)", cb, 1).Trim();

                            nonqueryCommand = myDataConnection.CreateCommand();
                            nonqueryCommand.CommandText = "INSERT  INTO review (appid,app_title, title, rate,rate_by,rate_date,content,vote_count,vote_sum,version,device_info) VALUES (@appid,@app_title, @title, @rate,@rate_by,@rate_date,@content,@vote_count,@vote_sum,@version,@device_info)";
                            nonqueryCommand.Parameters.AddWithValue("@appid", appid);
                            nonqueryCommand.Parameters.AddWithValue("@app_title", title);
                            nonqueryCommand.Parameters.AddWithValue("@title", rate_title);
                            nonqueryCommand.Parameters.AddWithValue("@rate", rate_rate);
                            nonqueryCommand.Parameters.AddWithValue("@rate_by", rate_by);
                            nonqueryCommand.Parameters.AddWithValue("@rate_date", rate_date);
                            nonqueryCommand.Parameters.AddWithValue("@content", rate_content);
                            nonqueryCommand.Parameters.AddWithValue("@vote_count", "");
                            nonqueryCommand.Parameters.AddWithValue("@vote_sum", "");
                            nonqueryCommand.Parameters.AddWithValue("@version", rate_version);
                            if (device.Length > 200)
                                device = "";
                            nonqueryCommand.Parameters.AddWithValue("@device_info", device);
                            nonqueryCommand.ExecuteNonQuery();
                            nonqueryCommand.Dispose();
                            review_count++;
                        }

                        if (current_page == total_pages)
                            break;
                    }
                    while (true);

                    nonqueryCommand = myDataConnection.CreateCommand();
                    nonqueryCommand.CommandText = "UPDATE app SET review_count=@review_count WHERE appid=@appid";
                    nonqueryCommand.Parameters.AddWithValue("review_count", review_count);
                    nonqueryCommand.Parameters.AddWithValue("appid", appid);
                    nonqueryCommand.ExecuteNonQuery();
                    nonqueryCommand.Dispose();
                }
                catch (Exception ex)
                {
                    Utility.WriteLog(ex.Message);
                    Utility.WriteLog(ex.StackTrace);
                }

            }
        }
    }
}

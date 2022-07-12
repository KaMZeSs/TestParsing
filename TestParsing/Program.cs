using System.Net;

using AngleSharp;
using AngleSharp.Html.Parser;

using System.Data.Entity;
using System.Data.SQLite;

namespace TestParsing
{
    class Program
    {
        static string[] urls =
        {
            "https://rezka.ag/films/",
            "https://rezka.ag/series/",
            "https://rezka.ag/cartoons/",
            "https://rezka.ag/animation/"
        };
        static string[] types =
        {
            "film",
            "series",
            "cartoons",
            "animation"
        };
        static string[] userAgent_list =
        {
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_5) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.1.1 Safari/605.1.15",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:77.0) Gecko/20100101 Firefox/77.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.97 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:77.0) Gecko/20100101 Firefox/77.0",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.97 Safari/537.36",
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/44.0.2403.157 Safari/537.36",
            "Mozilla/5.0 (X11; Ubuntu; Linux i686; rv:24.0) Gecko/20100101 Firefox/24.0",
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.121 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_13_6) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.1.2 Safari/605.1.15",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.1 Safari/605.1.15",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.1 Safari/605.1.15",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_6) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/12.1.2 Safari/605.1.15",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1 Safari/605.1.15",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/12.1 Safari/605.1.15",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_6) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.1 Safari/605.1.15",
            "Mozilla/5.0 (X11; CrOS x86_64 14324.72.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.91 Safari/537.36",
            "Mozilla/5.0 (X11; CrOS armv7l 7077.134.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/44.0.2403.156 Safari/E7FBAF",
            "Mozilla/5.0 (X11; CrOS x86_64 6680.78.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.102 Safari/E7FBAF",
            "Mozilla/5.0 (X11; CrOS armv7l 6946.86.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.134 Safari/E7FBAF",
            "Mozilla/5.0 (Series40; Nokia200/11.56; Profile/MIDP-2.1 Configuration/CLDC-1.1) Gecko/20100401 S40OviBrowser/2.0.1.62.6",
            "Mozilla/5.0 (Series40; Nokia200/11.81; Profile/MIDP-2.1 Configuration/CLDC-1.1) Gecko/20100401 S40OviBrowser/2.0.2.68.14",
            "Mozilla/5.0 (Linux; Android 5.0.2; Lenovo S60-a Build/LRX22G) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/56.0.2924.87 YaBrowser/17.3.1.383.00 Mobile Safari/E7FBAF"
        };

        static string userAgent_current;
        static Random random;

        static SQLiteConnection sqlite_conn;

        public static void Main()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance); //Нужно для чтения windows-1251 (юзается на некоторых сайтах)
            random = new();
            userAgent_current = userAgent_list[random.Next(userAgent_list.Length)];
            sqlite_conn = CreateDB();

            var time = DateTime.Now;
            try
            {
                for (int i = 0, counter = 0; i < urls.Length; i++)
                {
                    Console.WriteLine(types[i] + " начаты");
                    try
                    {
                        int task_i = i;
                        ProcessWork(urls[task_i], types[task_i]).Wait();
                    }
                    catch (Exception exc)
                    {
                        if (counter is 5)
                            continue;
                        i--;
                        counter++;
                    }
                    Task.Delay(50).Wait();
                    
                    Console.WriteLine(types[i] + " закончены");
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
            Console.WriteLine($"Обработка завершена за {(DateTime.Now - time).TotalSeconds} сек.");
            
        }

        public static async Task ProcessWork(String url, string type)
        {
            HttpClientHandler handler = new()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", userAgent_list[random.Next(userAgent_list.Length)]); 
            
            var response = client.GetAsync(url).Result;
            var html = await response.Content.ReadAsStringAsync();

            var parser = new HtmlParser();
            var doc = parser.ParseDocument(html);

            int last_page = GetLastPageNum(doc);

            DateTime? time = null;

            for (int i = 1, checker = 0, http_checker = 0; i <= last_page; i++)
            {
                FilmInfo[] films = new FilmInfo[0];
                try
                {
                    time = checker is 0 ? DateTime.Now : time is not null ? time : DateTime.Now;

                    client.DefaultRequestHeaders.Remove("User-Agent");
                    client.DefaultRequestHeaders.Add("User-Agent",
                        userAgent_list[random.Next(userAgent_list.Length)]);

                    var current_url = i is 1 ? url : url + $"page/{i}/";

                    if (i is 1)
                    {
                        films = await ProcessPage(doc, type);

                        if (films.Length is 0)
                        {
                            if (checker is 5)
                            {
                                Console.WriteLine($"Страница №{i} пропущена - {current_url}");
                            }
                            i--;
                            checker++;
                            Task.Delay(2000).Wait();
                            continue;
                        }

                        Console.WriteLine($"Страница №{i} закончена - {films.Length} - {(DateTime.Now - time)?.TotalSeconds}");

                        checker = 0;
                        WriteToDB(sqlite_conn, films);
                        continue;
                    }

                    response = client.GetAsync(current_url).Result;
                    html = await response.Content.ReadAsStringAsync();

                    doc = parser.ParseDocument(html);

                    films = await ProcessPage(doc, type);

                    if (films.Length is 0)
                    {
                        if (checker is 5)
                        {
                            checker = 0;
                            Console.WriteLine($"Страница №{i} пропущена - {current_url}");
                            continue;
                        }
                        i--;
                        checker++;
                        Task.Delay(2000).Wait();
                        continue;
                    }

                    Console.WriteLine($"Страница №{i} закончена - {films.Length} - {(DateTime.Now - time)?.TotalSeconds}");

                    checker = 0;
                }
                catch (Exception exc)
                {
                    if (http_checker is 5)
                    {
                        http_checker = 0;
                        continue;
                    }
                    i--;
                    http_checker++;
                }
                http_checker = 0;
                
                WriteToDB(sqlite_conn, films);
            }
        }

        static int GetLastPageNum(AngleSharp.Html.Dom.IHtmlDocument doc)
        {
            var pages = doc.GetElementsByClassName("b-navigation");
            var vs1 = pages[0];
            var vs2 = vs1.GetElementsByTagName("A");
            var last_page = vs2[vs2.Length - 2];
            return int.Parse(last_page.TextContent);
        }

        static async Task<FilmInfo[]> ProcessPage(AngleSharp.Html.Dom.IHtmlDocument doc, string type)
        {
            List<FilmInfo> list = new();

            //Ищу все ссылки на фильмы на странице
            var divs = doc.GetElementsByClassName("b-content__inline_item-link");

            var tasks = new Task<FilmInfo>[divs.Length];
            for (int i = 0; i < divs.Length; i++)
            {
                int i_to_task = i;
                tasks[i] = Task<FilmInfo>.Run(() =>
                {
                    return ProcessFilmPage(divs[i_to_task], type);
                });
            }
            Task.WaitAny(tasks);
            for (int i = 0; i < tasks.Length; i++)
            {
                Task.WaitAll(tasks[i]);
                list.Add(tasks[i].Result);
            }

            return list.ToArray();
        }

        static FilmInfo ProcessFilmPage(AngleSharp.Dom.IElement div, string type)
        {
            var date_country_data = div.Children[1].TextContent.Trim().Split(',');

            var title = div.Children[0].TextContent.Trim();
            var link = div.Children[0].GetAttribute("href");
            var country = date_country_data[1].Trim();

            var inf = GetFilmInfo(link);

            return new FilmInfo()
            {
                Title = title,
                Link = link,
                ReleaseDate = inf?.releaseDate,
                Country = country,
                Type = type,
                Genres = inf?.genres,
                Producer = inf?.producer,
            };
        }

        struct Information
        {
            public string releaseDate;
            public string producer;
            public string genres;

            public Information(string releaseDate, string producer, string genres)
            {
                this.releaseDate = releaseDate;
                this.producer = producer;
                this.genres = genres;
            }
        }

        static Information? GetFilmInfo(string url)
        {
            for (int j = 0; ; j++)
            {
                HttpClientHandler handler = new()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };

                var client = new HttpClient(handler);
                string html = string.Empty;

                bool isContinue = true;
                for (int i = 0; isContinue; i++)
                {
                    client.DefaultRequestHeaders.Add("User-Agent",
                        userAgent_list[random.Next(userAgent_list.Length)]);

                    var response = client.GetAsync(url).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        isContinue = false;
                    }
                    else
                    {
                        Task.Delay(200);
                        if (i is 100)
                            return null;
                        continue;
                    }
                    html = response.Content.ReadAsStringAsync().Result;
                }

                var parser = new HtmlParser();
                var doc = parser.ParseDocument(html);

                //table info
                var trs = doc.GetElementsByClassName("b-post__info")[0].GetElementsByTagName("tr");

                Dictionary<string, AngleSharp.Dom.IElement> dict = new();

                if (trs.Length is 0)
                {
                    Task.Delay(300);
                    if (j is 10)
                        return null;
                    continue;
                }

                for (int i = 0; i < trs.Length; i++)
                {
                    try
                    {
                        var res = IsInfo(trs[i].GetElementsByTagName("h2")[0].TextContent);
                        if (res is 1)
                        {
                            dict.Add("date", trs[i]);
                        }
                        else if (res is 2)
                        {
                            dict.Add("producer", trs[i]);
                        }
                        else if (res is 3)
                        {
                            dict.Add("genres", trs[i]);
                        }
                    }
                    catch (Exception exc)
                    {

                    }
                }
                string release, producer, genres;
                release = producer = genres = string.Empty;
                try
                {
                    release = dict["date"].GetElementsByTagName("td")[1].TextContent.Split(" год")[0];
                }
                catch (Exception exc)
                {

                }
                try
                {
                    producer = dict["producer"].GetElementsByTagName("a")[0].FirstChild.TextContent;
                }
                catch (Exception exc)
                {

                }
                try
                {
                    genres = String.Join(", ", from cur in dict["genres"].GetElementsByTagName("span") select cur.TextContent);
                }
                catch (Exception exc)
                {

                }

                return new Information(releaseDate: release, producer: producer, genres: genres);
            }
        }

        static int IsInfo(string text)
        {
            if (text.StartsWith("Дата"))
            {
                return 1;
            }
            if (text.StartsWith("Режиссер"))
            {
                return 2;
            }
            if (text.StartsWith("Жанр"))
            {
                return 3;
            }
            return -1;
        }


        static void WriteToDB(SQLiteConnection conn, FilmInfo[] films)
        {
            try
            {
                conn.Open();

                SQLiteCommand sqlite_cmd;
                sqlite_cmd = conn.CreateCommand();

                foreach (var film in films)
                {
                    try
                    {
                        sqlite_cmd.CommandText = 
                            @$"INSERT INTO Films (Title, Release, Country, Link, Type, Producer, Genres) 
                            VALUES ('{film.Title}', '{film.ReleaseDate}', '{film.Country}', '{film.Link}',
                            '{film.Type}', '{film.Producer}', '{film.Genres}');";
                        sqlite_cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                        var normal_title = film.Title.Replace("\"", "&quot").Replace("\'", "&apos;");
                        var normal_country = film.Country?.Replace("\"", "&quot").Replace("\'", "&apos;");
                        var normal_producer = film.Producer?.Replace("\"", "&quot").Replace("\'", "&apos;");
                        sqlite_cmd.CommandText =
                            @$"INSERT INTO Films (Title, Release, Country, Link, Type, Producer, Genres) 
                            VALUES ('{normal_title}', '{film.ReleaseDate}', '{(normal_country is null ? "null" : normal_country)}', '{film.Link}',
                            '{film.Type}', '{(normal_producer is null ? "null" : normal_producer)}', '{film.Genres}');";
                        sqlite_cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                conn.Close();
            }
        }
        static SQLiteConnection CreateDB()
        {
            SQLiteConnection sql = new("Data Source=database.db; Version = 3; New = True; Synchronous=Off");
            CreateTable(sql);
            return sql;
        }
        static void CreateTable(SQLiteConnection conn)
        {
            try
            {
                conn.Open();
                SQLiteCommand sqlite_cmd;
                string checkExistence = "SELECT title FROM FILMS LIMIT 1";
                string Createsql =
                    @$"CREATE TABLE Films (
                        Title TEXT, 
                        Release TEXT, 
                        Country TEXT, 
                        Link TEXT, 
                        Type TEXT,
                        Producer TEXT,
                        Genres TEXT
                    )";
                sqlite_cmd = conn.CreateCommand();
                sqlite_cmd.CommandText = checkExistence;
                try
                {
                    sqlite_cmd.ExecuteScalar();
                }
                catch (Exception)
                {
                    sqlite_cmd.CommandText = Createsql;
                    sqlite_cmd.ExecuteNonQuery();
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
            finally
            {
                conn.Close();
            }
        }
    }
}
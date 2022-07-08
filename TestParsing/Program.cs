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
                for (int i = 0; i < urls.Length; i++)
                {
                    Console.WriteLine(types[i] + " начаты");
                    ProcessWork(urls[i], types[i]);
                    Console.WriteLine(types[i] + " закончены");
                }
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
            }
            Console.WriteLine($"Обработка завершена за {(DateTime.Now - time).TotalSeconds} сек.");
            
        }

        public static async void ProcessWork(String url, string type)
        {
            HttpClientHandler handler = new()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", userAgent_current); 
            
            var response = client.GetAsync(url).Result;
            var html = await response.Content.ReadAsStringAsync();

            var parser = new HtmlParser();
            var doc = parser.ParseDocument(html);

            int last_page = GetLastPageNum(doc);

            List<FilmInfo> all_films = new();

            for (int i = 1, checker = 0; i <= last_page; i++)
            {
                client.DefaultRequestHeaders.Remove("User-Agent");
                client.DefaultRequestHeaders.Add("User-Agent", 
                    userAgent_list[random.Next(userAgent_list.Length)]);

                FilmInfo[] films;
                if (i is 1)
                {
                    films = await ProcessPage(doc, type);

                    Console.WriteLine($"Страница №{i} закончена - {films.Length}");
                    continue;
                }

                var current_url = i is 1 ? url : url + $"page/{i}/";
                
                response = client.GetAsync(current_url).Result;
                html = await response.Content.ReadAsStringAsync();

                doc = parser.ParseDocument(html);

                films = await ProcessPage(doc, type);

                Console.WriteLine($"Страница №{i} закончена - {films.Length}");

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

                checker = 0;
                all_films.AddRange(films);
                WriteToDB(sqlite_conn, films);
            }
        }

        static int GetLastPageNum(AngleSharp.Html.Dom.IHtmlDocument doc)
        {
            var pages = doc.GetElementsByClassName("b-navigation")[0].GetElementsByTagName("A");
            var last_page = pages[pages.Length - 2];
            return int.Parse(last_page.TextContent);
        }
        static async Task<FilmInfo[]> ProcessPage(AngleSharp.Html.Dom.IHtmlDocument doc, string type)
        {
            List<FilmInfo> list = new();

            //Ищу все ссылки на фильмы на странице
            var divs = doc.GetElementsByClassName("b-content__inline_item-link");

            foreach (var div in divs)
            {
                list.Add(ProcessFilmPage(div, type));
            }

            //Оказалось быстрее в одном потоке

            //var tasks = new Task<FilmInfo>[divs.Length];
            //for (int i = 0; i < divs.Length; i++)
            //{
            //    int i_to_task = i;
            //    tasks[i] = Task<FilmInfo>.Run(() =>
            //    {
            //        return ProcessFilmPage(divs[i_to_task]);
            //    });
            //}
            //Task.WaitAny(tasks);
            //foreach (var task in tasks)
            //{
            //    list.Add(task.Result);
            //}


            return list.ToArray();
        }
        static FilmInfo ProcessFilmPage(AngleSharp.Dom.IElement div, string type)
        {
            var date_country_data = div.Children[1].TextContent.Trim().Split(',');

            var title = div.Children[0].TextContent.Trim();
            var link = div.Children[0].GetAttribute("href");
            var release_date = DateTime.Parse("01.01." + date_country_data[0].Split('-')[0].Trim());
            var country = date_country_data[1].Trim();

            return new FilmInfo()
            {
                Title = title,
                Link = link,
                ReleaseDate = release_date,
                Country = country,
                Type = type
            };

            //Позже для фул инфы
            //var user_agent = userAgent_list[random.Next(userAgent_list.Length)];
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
                    var date = film.ReleaseDate?.ToString("dd-MM-yyyy");
                    try
                    {
                        sqlite_cmd.CommandText = "INSERT INTO Films (Title, Release, Country, Link, Type)" +
                            $" VALUES('{film.Title}', '{date}', '{film.Country}', '{film.Link}', '{film.Type}');";
                        sqlite_cmd.ExecuteNonQuery();
                    }
                    catch
                    {
                        var normal_title = film.Title.Replace("\"", "&quot").Replace("\'", "&apos;");
                        sqlite_cmd.CommandText = "INSERT INTO Films (Title, Release, Country, Link, Type)" +
                            $" VALUES('{normal_title}', '{date}', '{film.Country}', '{film.Link}', '{film.Type}');";
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
            if (File.Exists("database.db"))
            {
                File.Delete("database.db");
            }

            SQLiteConnection sql = new("Data Source=database.db; Version = 3; New = True; Compress = True; Synchronous=Off");
            CreateTable(sql);
            return sql;
        }
        static void CreateTable(SQLiteConnection conn)
        {
            try
            {
                conn.Open();
                SQLiteCommand sqlite_cmd;
                string Createsql = "CREATE TABLE Films (Title TEXT, Release TEXT, Country TEXT, Link TEXT, Type TEXT)";
                sqlite_cmd = conn.CreateCommand();
                sqlite_cmd.CommandText = Createsql;
                sqlite_cmd.ExecuteNonQuery();
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
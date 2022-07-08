namespace TestParsing
{
    internal class FilmInfo
    {
        string title;
        string? releaseDate;
        string? country;
        string? producer;
        string? genres;
        string link;
        string? type;
        

        #region Properties
       
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                title = value;
            }
        }

        public string? ReleaseDate
        {
            get
            {
                return releaseDate;
            }
            set
            {
                releaseDate = value;
            }
        }
        public string? Country
        {
            get
            {
                return country;
            }
            set
            {
                country = value;
            }
        }
        public string? Producer
        {
            get
            {
                return producer;
            }
            set
            {
                producer = value;
            }
        }
        public string? Genres
        {
            get
            {
                return genres;
            }
            set
            {
                genres = value;
            }
        }
        public string Link
        {
            get
            {
                return link;
            }
            set
            {
                link = value;
            }
        }
        public string? Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }

        #endregion
    }
}

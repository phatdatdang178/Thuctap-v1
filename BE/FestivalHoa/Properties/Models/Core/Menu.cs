using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using FestivalHoa.Properties.Constants;

namespace FestivalHoa.Properties.Models.Core
{
   
    public class Menu : Audit, TEntity<string>
    {
        public string Path { get; set; }
        public string Icon { get; set; }


        /*
        private string _resource;

        public string Resource
        {
            get
            {
                return  FormatterString.ConvertToUnsign(Name).Replace(" ", "");
            }
            set
            {
            }
        }*/
        public string ParentId { get; set; }
        public int Level { get; set; } = 0;
        public int Sort { get; set; }



        public List<string> ListAction
        {
            get;
            set;
        } = new List<string>();



        public void SetListAction(string Name)
        {
            foreach (var item in ListActionDefault.listAction)
            {
                var actionName = item + "-" + FormatterString.ConvertToUnsign(Name).Replace(" ", "");
                this.ListAction.Add(actionName);
            }
        }
        
        
        [BsonIgnore]
        public bool IsChecked { get; set; } = false;
        
        
        
    }

    
    
    
    
    
    

    public class MenuShort
    {
        public string Id { get; set; }
        
        public string Name { get; set; }
        public string Path { get; set; }
        public string Icon { get; set; }
        public string ParentId { get; set; }
        public int Sort { get; set; }

        public MenuShort(Menu menu)
        {
            this.Id = menu.Id;
            this.Name = menu.Name;
            this.Path = menu.Path;
            this.ParentId = menu.ParentId;
            this.Icon = menu.Icon;
            this.Sort = menu.Sort;
        }

    }
    public class MenuList
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        [JsonProperty(PropertyName = "_id")]
        public string Id { get; set; }
        public string ListAction { get; set; }
    }
    public class MenuListShort
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Resource
        {
            get
            {
                return FormatterString.ConvertToUnsign(Name).Replace(" ", "");
            }
            set
            {
            }
        }
        public string ListAction { get; set; }
    }
    public class MenuListShort1
    {
        [BsonId]
        [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> ListAction { get; set; }
    }
}
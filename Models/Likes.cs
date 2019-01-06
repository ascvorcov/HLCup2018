namespace hlcup2018.Models
{
  public class Likes
  {
    public Like[] likes;
    public class Like
    {
      public int ts;
      public int likee;
      public int liker;
    }
  }
}
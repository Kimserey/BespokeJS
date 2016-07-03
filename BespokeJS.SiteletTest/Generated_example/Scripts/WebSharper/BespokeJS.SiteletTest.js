(function()
{
 var Global=this,Runtime=this.IntelliFactory.Runtime,UI,Next,Doc;
 Runtime.Define(Global,{
  BespokeJS:{
   SiteletTest:{
    Client:{
     page:function(hello)
     {
      var matchValue;
      matchValue=hello.Hello;
      return matchValue.$==1?Doc.TextNode("Hi, "+hello.Greeting):matchValue.$==2?Doc.TextNode("Hey, "+hello.Greeting):Doc.TextNode("Hello, "+hello.Greeting);
     }
    }
   }
  }
 });
 Runtime.OnInit(function()
 {
  UI=Runtime.Safe(Global.WebSharper.UI);
  Next=Runtime.Safe(UI.Next);
  return Doc=Runtime.Safe(Next.Doc);
 });
 Runtime.OnLoad(function()
 {
  return;
 });
}());

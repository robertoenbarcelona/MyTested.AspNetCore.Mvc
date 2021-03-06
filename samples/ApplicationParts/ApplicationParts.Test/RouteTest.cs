﻿namespace ApplicationParts.Test
{
    using Controllers;
    using MyTested.AspNetCore.Mvc;
    using Xunit;

    public class RouteTest
    {
        [Fact]
        public void HomeIndexShouldMatchCorrectController()
        {
            MyRouting
                .Configuration()
                .ShouldMap("/")
                .To<HomeController>(c => c.Index());
        }
    }
}

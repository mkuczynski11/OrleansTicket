using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orleans.TestingHost;
using OrleansTicket.Actors;
using OrleansTicket.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrleansTicket.Tests.Tests
{
    [TestClass()]
    public class UserTestsTests
    {
        [TestMethod()]
        public async void User_actor_must_reply_with_its_dataTest()
        {
            var builder = new TestClusterBuilder();
            var cluster = builder.Build();
            cluster.Deploy();

            var user = cluster.GrainFactory.GetGrain<IUserGrain>("martin@gmail.com");
            await user.InitializeUser("Martin", "Kuczynski");

            var userInfo = await user.GetUserInfo();

            cluster.StopAllSilos();

            Assert.Equals("Martin", userInfo.UserDetails.Name);
            Assert.Equals("Kuczynski", userInfo.UserDetails.Surname);
            Assert.IsTrue(userInfo.UserDetails.IsInitialized);
        }
    }
}
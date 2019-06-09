using System;
using System.Security.Claims;
using System.Web.Http;
using App.Identity;
using App.ViewModels;
using log4net;
using Microsoft.Owin.Infrastructure;
using Microsoft.Owin.Security;
using App.Common;
using App.Plugins;
using System.Data;
using System.Text;
using System.Collections.Generic;

namespace App.Controllers
{
    [RoutePrefix("api/Account")]
    public class AccountController : ApiController
    {
        [HttpPost, Route("Token")]
        public IHttpActionResult Token(LoginViewModel login)
        {
            if (!ModelState.IsValid)
            {
                return this.BadRequestError(ModelState);
            }
            ClaimsIdentity identity;
            if (!_loginProvider.ValidateCredentials(login.UserName, login.Password, out identity))
            {
                return BadRequest("Incorrect user or password");
            }
            DataSet data = DBConnection.GetQuery(@"SELECT [id],[surname],[name],[middle_name],[email]
                                                FROM[ReportServer].[dbo].[Users]
                                                where[login] = '" + login.UserName + "'");
            var user = new AccountProfileViewModel { id = Convert.ToInt32(data.Tables[0].Rows[0][0]), surname = data.Tables[0].Rows[0][1].ToString(), name = data.Tables[0].Rows[0][2].ToString(), middlename = data.Tables[0].Rows[0][3].ToString(), email = data.Tables[0].Rows[0][4].ToString(), Login = login.UserName };
            var ticket = new AuthenticationTicket(identity, new AuthenticationProperties());
            var currentUtc = new SystemClock().UtcNow;
            ticket.Properties.IssuedUtc = currentUtc;
            ticket.Properties.ExpiresUtc = currentUtc.Add(TimeSpan.FromDays(7));
            return Ok(new LoginAccessViewModel
            {
                User = user,
                AccessToken = Startup.OAuthOptions.AccessTokenFormat.Protect(ticket)
            });
        }

        [HttpPost, Route("Registry")]
        public IHttpActionResult Registry(AccountRegistryViewModel account)
        {
            if (!ModelState.IsValid)
            {
                return this.BadRequestError(ModelState);
            }
            DataSet data = DBConnection.GetQuery("select max(id) from [ReportServer].[dbo].[Users]");
            string id = "0";
            if (data.Tables[0].Rows[0][0].ToString() != "")
            {
                id = data.Tables[0].Rows[0][0].ToString();
            }
            data = DBConnection.GetQuery("select count(*) from [ReportServer].[dbo].[Users] where Login = '" + account.Login + "'");
            if (Convert.ToInt32(data.Tables[0].Rows[0][0].ToString()) > 0)
            {
                return BadRequest("User login already exist");
            }
            string query = @"insert into [ReportServer].[dbo].[Users] values (" + (Convert.ToInt32(id) + 1) + ",'" + account.surname + "','" + account.name + "','" + account.middlename + "','" + account.email + "','" + account.Login + "','" + Plugins.MD5.CreateMD5(account.Password) + "')";
            data = DBConnection.GetQuery(query);
            if (data == null)
            {
                return BadRequest("DB query error");
            }
            account.Password = "";
            return Ok();
        }

        /// <summary>
        /// Use this action to test authentication
        /// </summary>
        /// <returns>status code 200 if the user is authenticated, otherwise status code 401</returns>
        [Authorize]
        [HttpGet, Route("Ping")]
        public IHttpActionResult Ping()
        {
            return Ok();
        }

        [Authorize]
        [HttpGet, Route("UserList")]
        public IHttpActionResult UserList()
        {
            DataSet data = DBConnection.GetQuery(@"select [id]
                                                          ,[surname]
                                                          ,[name]
                                                          ,[middle_name]
                                                          ,[email]
                                                          ,[login] from [ReportServer].[dbo].[Users]");
            if (data == null)
            {
                return BadRequest("Not connect to DB");
            }
            List<AccountProfileViewModel> list = new List<AccountProfileViewModel>();
            foreach (DataRow row in data.Tables[0].Rows)
            {
                list.Add(new AccountProfileViewModel { id = Convert.ToInt32(row[0].ToString()), surname = row[1].ToString(), name = row[2].ToString(), middlename = row[3].ToString(), email = row[4].ToString(), Login = row[5].ToString() });
            }
            return Ok(list);
        }

        [Authorize]
        [HttpDelete, Route("UserDelete")]
        public IHttpActionResult UserDelete(int id)
        {
            if (id == 1)
            {
                return BadRequest("Admin account not deleted");
            }
            DataSet data = DBConnection.GetQuery(@"Delete [ReportServer].[dbo].[Users] where id = "+id+"");
            if (data == null)
            {
                return BadRequest("Not connect to DB");
            }
            return Ok();
        }

        [Authorize]
        [HttpPut, Route("UserChangePassword")]
        public IHttpActionResult UserChangePassword(AccountRegistryViewModel login)
        {
            if (!ModelState.IsValid)
            {
                return this.BadRequestError(ModelState);
            }
            DataSet data = DBConnection.GetQuery("update [ReportServer].[dbo].[Users] set [password]='"+MD5.CreateMD5(login.Password)+ "' where [login]='"+login.Login+"'");
            if (data == null)
            {
                return BadRequest("Not connect to DB");
            }
            return Ok();
        }

        [Authorize]
        [HttpPut, Route("UserChange")]
        public IHttpActionResult UserChange(AccountProfileViewModel login)
        {
            if (!ModelState.IsValid)
            {
                return this.BadRequestError(ModelState);
            }
            DataSet data = DBConnection.GetQuery("select [login] from [ReportServer].[dbo].[Users] where [id]="+login.id+"");
            if (data == null)
            {
                return BadRequest("Not connect to DB");
            }
            if (data.Tables[0].Rows[0][0].ToString() != login.Login)
            {
                data = DBConnection.GetQuery("select * from [ReportServer].[dbo].[Users] where [login]='"+login.Login+"'");
                if (data == null)
                {
                    return BadRequest("Not connect to DB");
                }
                if (data.Tables[0].Rows.Count != 0)
                {
                    return BadRequest("This login already used");
                }
                data = DBConnection.GetQuery("update [ReportServer].[dbo].[Users] set [login]='"+login.Login+"' where [id]=" + login.id + "");
                if (data == null)
                {
                    return BadRequest("Not connect to DB");
                }
            }
            data = DBConnection.GetQuery("update [ReportServer].[dbo].[Users] set [surname]='"+login.surname+ "', [name]='"+login.name+ "', [middle_name]='"+login.middlename+ "', [email]='"+login.email+"' where [id]=" + login.id);
            if (data == null)
            {
                return BadRequest("Not connect to DB");
            }
            return Ok();
        }
        public AccountController(ILoginProvider loginProvider)
        {
            Log.Debug("Entering AccountController()");
            _loginProvider = loginProvider;
        }

        private readonly ILoginProvider _loginProvider;
        private static readonly ILog Log = LogManager.GetLogger(typeof(AccountController));
    }
}

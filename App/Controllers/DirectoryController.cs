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
using System.Linq;

namespace App.Controllers
{
    [RoutePrefix("api/Directory")]
    public class DirectoryController : ApiController
    {
        [Authorize]
        [HttpPut, Route("CreateDirectory")]
        public IHttpActionResult CreateDirectory(DirectoryAddModel directory)
        {
            if (!ModelState.IsValid)
            {
                return this.BadRequestError(ModelState);
            }
            DataSet data = DBConnection.GetQuery("select count(*) from [ReportServer].[dbo].[Urls] where [url]='"+directory.directoryPrefics+"/"+directory.directoryName+"'");
            if (data == null)
            {
                return BadRequest("Not connect to DB");
            }
            if (Convert.ToInt32(data.Tables[0].Rows[0][0]) > 0)
            {
                return BadRequest("Directory alredy exist");
            }
            data = DBConnection.GetQuery("select max([id]) from [ReportServer].[dbo].[Urls]");
            string id = "0";
            if (data.Tables[0].Rows[0][0].ToString() != "")
            {
                id = data.Tables[0].Rows[0][0].ToString();
            }
            data = DBConnection.GetQuery("insert into [ReportServer].[dbo].[Urls] values ("+(Convert.ToInt32(id)+1).ToString()+",'" + directory.directoryPrefics + "/"+directory.directoryName+"',0,null)");
            if (data == null)
            {
                return BadRequest("Not connect to DB");
            }
            return Ok();
        }
        [Authorize]
        [HttpGet, Route("GetDirectory")]
        public IHttpActionResult GetDirectory(string directory)
        {
            if (!ModelState.IsValid)
            {
                return this.BadRequestError(ModelState);
            }
            DataSet data = DBConnection.GetQuery(@"select [id]
                                                  ,[url]
                                                  ,[isReport]
                                                  ,[reportId] from [ReportServer].[dbo].[Urls] where [url] like '"+directory+"%'");
            if (data == null)
            {
                return BadRequest("Not connect to DB");
            }
            List<DirectoryViewModel> dirs = new List<DirectoryViewModel>();
            foreach (DataRow row in data.Tables[0].Rows)
            {
                int i = row[1].ToString().Split('/').Length;
                int j = directory.Split('/').Length;
                if (i == j+1)
                { 
                    dirs.Add(new DirectoryViewModel { id = Convert.ToInt32(row[0]), url = row[1].ToString(), isReport = Convert.ToInt32(row[2]), reportId = string.IsNullOrEmpty(row[3].ToString()) ? 0 : Convert.ToInt32(row[3]) });
                }
            }
            dirs = dirs.OrderBy(f => f.url).OrderBy(f => f.isReport).ToList();
            return Ok(dirs);
        }
        [Authorize]
        [HttpPost, Route("DeleteDirectory")]
        public IHttpActionResult DeleteDirectory(DirectoryViewModel directory)
        {
            if (!ModelState.IsValid)
            {
                return this.BadRequestError(ModelState);
            }
            DataSet data = new DataSet();
            if (directory.isReport == 1)
            {
                data = DBConnection.GetQuery("delete [ReportServer].[dbo].[Urls] where id = " + directory.id);
                data = DBConnection.GetQuery("delete [ReportServer].[dbo].[Report] where id = " + directory.reportId);
            }
            else
            {
                data = DBConnection.GetQuery(@"SELECT [id]
                                                          ,[url]
                                                          ,[isReport]
                                                          ,[reportId]
                                                      FROM [ReportServer].[dbo].[Urls] where [url] like '" + directory.url + "/%'");
                foreach (DataRow row in data.Tables[0].Rows)
                {
                    data = DBConnection.GetQuery("delete [ReportServer].[dbo].[Urls] where id = " + row[0]);
                    if (row[3].ToString() != "")
                    {
                        data = DBConnection.GetQuery("delete [ReportServer].[dbo].[Report] where id = " + row[3]);
                    }
                }
                data = DBConnection.GetQuery("delete [ReportServer].[dbo].[Urls] where id = " + directory.id);
            }
            return Ok();
        }
    }
}

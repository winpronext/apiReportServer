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
            return Ok(dirs);
        }
    }
}

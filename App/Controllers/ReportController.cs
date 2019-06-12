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
    [RoutePrefix("api/Report")]
    public class ReportController : ApiController
    {
        [Authorize]
        [HttpGet, Route("GetTypeSource")]
        public IHttpActionResult GetTypeSource()
        {
            DataSet data = DBConnection.GetQuery(@"SELECT [id]
                                                          ,[DBType]
                                                      FROM [ReportServer].[dbo].[DBType]");
            if (data == null)
            {
                return BadRequest("Not connect to DB");
            }
            List<TypeSourceViewModel> model = new List<TypeSourceViewModel>();
            foreach (DataRow row in data.Tables[0].Rows)
            {
                model.Add(new TypeSourceViewModel {id = Convert.ToInt32(row[0]), DBType = row[1].ToString() });
            }
            return Ok(model);
        }

        [Authorize]
        [HttpGet, Route("GetSource")]
        public IHttpActionResult GetSource()
        {
            DataSet data = DBConnection.GetQuery(@"select [id]
                                                          ,[name]
	                                                      from [ReportServer].[dbo].[Source]");
            if (data == null)
            {
                return BadRequest("Not connect to DB");
            }
            List<SourceViewModel> model = new List<SourceViewModel>();
            foreach (DataRow row in data.Tables[0].Rows)
            {
                model.Add(new SourceViewModel { id = Convert.ToInt32(row[0]), name = row[1].ToString() });
            }
            return Ok(model);
        }
        [Authorize]
        [HttpPost, Route("TestSelect")]
        public IHttpActionResult TestSelect(ReportAdd report)
        {
            if (!ModelState.IsValid)
            {
                return this.BadRequestError(ModelState);
            }
            if (string.IsNullOrEmpty(report.query))
            {
                return BadRequest("Query is empty");
            }
            DataSet data = DBConnection.GetQuery(@"SELECT [id]
                                                          ,[name]
                                                          ,[server]
                                                          ,[db]
                                                          ,[login]
                                                          ,[password]
                                                          ,[typeId]
                                                      FROM [ReportServer].[dbo].[Source]
                                                      where [id] = " + report.source.id);
            if (data == null)
            {
                return BadRequest("Not connect to DB");
            }
            string connString = "Data Source="+data.Tables[0].Rows[0][2].ToString()+";Initial Catalog="+ data.Tables[0].Rows[0][3].ToString() + ";User ID="+ data.Tables[0].Rows[0][4].ToString() + ";Password="+ data.Tables[0].Rows[0][5].ToString() + "";
            data = DBConnection.GetQuery(report.query, connString);
            if (data == null)
            {
                return BadRequest("Not connect to DB");
            }
            return Ok();
        }
    }
}

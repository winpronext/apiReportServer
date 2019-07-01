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
    [RoutePrefix("api/Source")]
    public class SourceController : ApiController
    {
        [Authorize]
        [HttpGet, Route("GetSource")]
        public IHttpActionResult GetSource()
        {
            DataSet data = DBConnection.GetQuery(@"SELECT s.[id]
      ,[name]
      ,[server]
      ,[db]
      ,[login]
,[password]
      ,[typeId]
	  ,d.DBType
  FROM [ReportServer].[dbo].[Source] s
  left join [ReportServer].[dbo].[DBType] d on s.typeId = d.id");
            if (data == null)
            {
                return BadRequest("Not connect to DB");
            }
            List<SourceAllCreateViewModel> source = new List<SourceAllCreateViewModel>();
            foreach (DataRow row in data.Tables[0].Rows)
            {
                source.Add(new SourceAllCreateViewModel { id = Convert.ToInt32(row[0]), name = row[1].ToString(), server = row[2].ToString(), db = row[3].ToString(), login = row[4].ToString(), password = row[5].ToString(), typeSource = new TypeSourceViewModel { id = Convert.ToInt32(row[6]), DBType = row[7].ToString()} });
            }
            return Ok(source);
        }
        [Authorize]
        [HttpPost, Route("CreateSource")]
        public IHttpActionResult CreateSource(SourceAllCreateViewModel source)
        {
            if (!ModelState.IsValid)
            {
                return this.BadRequestError(ModelState);
            }
            DataSet data = DBConnection.GetQuery("select max([id]) from [ReportServer].[dbo].[Source]");
            string id = "0";
            if (data.Tables[0].Rows[0][0].ToString() != "")
            {
                id = data.Tables[0].Rows[0][0].ToString();
            }
            data = DBConnection.GetQuery(@"insert into [ReportServer].[dbo].[Source]
  values ("+ (Convert.ToInt32(id) + 1).ToString() + ",'"+source.name+"','"+source.server+"','"+source.db+"','"+source.login+"','"+source.password+"',"+source.typeSource.id+")");
            if (data == null)
            {
                return BadRequest("Not connect to DB");
            }
            return Ok();
        }
        [Authorize]
        [HttpDelete, Route("DeleteSource")]
        public IHttpActionResult DeleteSource(int id)
        {
            DataSet data = DBConnection.GetQuery(@"Delete [ReportServer].[dbo].[Source] where id = " + id + "");
            if (data == null)
            {
                return BadRequest("Not connect to DB");
            }
            return Ok();
        }

        [Authorize]
        [HttpPut, Route("ChangeSource")]
        public IHttpActionResult ChangeSource(SourceAllCreateViewModel source)
        {
            if (!ModelState.IsValid)
            {
                return this.BadRequestError(ModelState);
            }
            DataSet data = DBConnection.GetQuery(@"update [ReportServer].[dbo].[Source]
    set [name] = '"+source.name+@"', [server] = '"+source.server+"', [db] = '"+source.db+"', [login] = '"+source.login+"',[password] = '"+source.password+"', [typeId] = '"+source.typeSource.id+"' where [id] = "+source.id);
            if (data == null)
            {
                return BadRequest("Not connect to DB");
            }
            return Ok();
        }
    }
}

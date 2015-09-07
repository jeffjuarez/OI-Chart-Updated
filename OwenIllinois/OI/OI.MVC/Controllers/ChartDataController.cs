using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Drawing;
using System.Globalization;
using OI.Entities.Models;

using DotNet.Highcharts;
using DotNet.Highcharts.Enums;
using DotNet.Highcharts.Helpers;
using DotNet.Highcharts.Options;
using Point = DotNet.Highcharts.Options.Point;

using Repository.Pattern.Repositories;
using Repository.Pattern.UnitOfWork;
using Repository.Pattern.Infrastructure;
using OI.MVC.Models;


namespace OI.MVC.Controllers
{
    public class ChartDataController : Controller
    {

        private readonly IRepositoryAsync<Document> _documentRepositoryAsync;
        private readonly IUnitOfWorkAsync _unitOfWorkAsync;
       
        public ChartDataController(IUnitOfWorkAsync unitOfWorkAsync,
            IRepositoryAsync<Document> documentRepositoryAsync)
        {
            _documentRepositoryAsync = documentRepositoryAsync;
                        _unitOfWorkAsync = unitOfWorkAsync;
        }

     
        [HttpPost]
       public ActionResult Search_Productivity(FormCollection form)
     
            {
                  ChartFilterModel  objChartFilterModel = new ChartFilterModel();
                    
                    objChartFilterModel.SortCenter =  form["SortCenter"].ToString();
                  //  objChartFilterModel.Item = form["ProductivityItems"].ToString();
                    if (form["RecDateFrom"].ToString() != string.Empty && form["RecDateTo"].ToString() != string.Empty)
                    {     
                        objChartFilterModel.DateRecFrom = Convert.ToDateTime(form["RecDateFrom"].ToString());
                        objChartFilterModel.DateRecTo = Convert.ToDateTime(form["RecDateTo"].ToString());
                    }

                    TempData["ChartFilterModel"] = objChartFilterModel;// Assign to Temp Data

                  
                return RedirectToAction("Productivity", "ChartData"); // Reload with Filter
            }



        //Productivity - Chart
        public ActionResult Productivity()
        {


            var _ChartFiltermodel = TempData["ChartFilterModel"] as ChartFilterModel; // Getting the TempData ChartFilter Model

        
            var username = HttpContext.User.Identity.Name;

            var employee = _documentRepositoryAsync
                .GetRepository<Account>()
                .Query(a => a.Username == username)
                .Select(e => e.Employee)
                .SingleOrDefault();

            if (employee == null) return View();
            var viewModels = _documentRepositoryAsync
                 .Query(e => e.EmployeeId == employee.Id )
                .Select(d => new DocumentProductivityModel()
                {
                   SortCenter = d.SortCenter,
                   DateRec = d.ReceivedDateFrom,
                   Productivity_100n = d.Productivity_100n,
                   Productivity_201u = d.Productivity_201u,
                   Productivity_300u = d.Productivity_300u,
                   Productivity_400u = d.Productivity_400u,
                })
                .ToList();

            // APPLY FILTERING
     
            if (_ChartFiltermodel != null)
              {
                   if(_ChartFiltermodel.SortCenter != "All" && _ChartFiltermodel.DateRecFrom != Convert.ToDateTime("01/01/0001"))
                 {
                       ViewBag.ReportParam = "  Filter: ( " + _ChartFiltermodel.SortCenter + " Sort Center & Received From : " + _ChartFiltermodel.DateRecFrom.ToShortDateString() + "  to  " + _ChartFiltermodel.DateRecTo.ToShortDateString() + " ) ";
                       viewModels = viewModels.Where(x => x.SortCenter == _ChartFiltermodel.SortCenter && x.DateRec >= _ChartFiltermodel.DateRecFrom && x.DateRec <= _ChartFiltermodel.DateRecTo).ToList(); } 
                 else if( _ChartFiltermodel.SortCenter != "All" && _ChartFiltermodel.DateRecFrom == Convert.ToDateTime("01/01/0001"))
                 {
                     ViewBag.ReportParam = "  Filter: ( " + _ChartFiltermodel.SortCenter + " Sort Center Data ) ";
            
                      viewModels = viewModels.Where(x => x.SortCenter == _ChartFiltermodel.SortCenter).ToList();// SORT CENTER ONLY  
                 }

                   else if (_ChartFiltermodel.SortCenter == "All" && _ChartFiltermodel.DateRecFrom != Convert.ToDateTime("01/01/0001"))
                   {
                       ViewBag.ReportParam = "  Filter: ( Data from all Sort Centers  & Received From : " + _ChartFiltermodel.DateRecFrom.ToLongDateString() + "  to  " + _ChartFiltermodel.DateRecTo.ToLongDateString() + " ) ";

                       viewModels = viewModels.Where(x => x.DateRec >= _ChartFiltermodel.DateRecFrom && x.DateRec <= _ChartFiltermodel.DateRecTo).ToList(); // DATE RECEIVED FROM ONLY
                   }
             }


            string[] _strDateRec = new string[viewModels.Count];

            object[] _objDateRec = new object[viewModels.Count];
            object[] _obj100n = new object[viewModels.Count];
            object[] _obj201u = new object[viewModels.Count];
            object[] _obj300u = new object[viewModels.Count];
            object[] _obj400u = new object[viewModels.Count];

            int i = 0;

            foreach (DocumentProductivityModel item in viewModels)
            {
                
                _objDateRec.SetValue(item.DateRec.ToString("d MMM"), i);
                _obj100n.SetValue(item.Productivity_100n, i);
                _obj201u.SetValue(item.Productivity_201u, i);
                _obj300u.SetValue(item.Productivity_300u, i);
                _obj400u.SetValue(item.Productivity_400u, i);

                _strDateRec[i] = string.Format(item.DateRec.ToString("d MMM"), i);
                
                i++;
            }

        

            Highcharts chart = new Highcharts("chart")
                //.InitChart(new Chart { DefaultSeriesType = ChartTypes.Column })
                 .InitChart(new Chart
                 {
                     Type = ChartTypes.Column,
                     Margin = new[] { 80 },
                     Options3d = new ChartOptions3d
                     {
                         Enabled = true,
                         Alpha = 10,
                         Beta = 0,
                         Depth = 75
                     }
                 })

                .SetTitle(new Title { Text = "OI - Productivity " })
                //.SetSubtitle(new Subtitle { Text = "Source: Jeff@Drake.Com" })
             
                .SetXAxis(new XAxis { Categories = _strDateRec }) // The Date Received
                .SetYAxis(new YAxis
                {
                    Min = 0,
                    Title = new YAxisTitle { Text = "Items Sorted per Hour" }
                })
                .SetLegend(new Legend
                {
                    Layout = Layouts.Vertical,
                    Align = HorizontalAligns.Left,
                    VerticalAlign = VerticalAligns.Top,
                    X = 50,
                    Y = 10,
                    Floating = true,
                    BackgroundColor = new BackColorOrGradient(ColorTranslator.FromHtml("#FFFFFF")),
                    Shadow = true
                })
                .SetTooltip(new Tooltip { Formatter = @"function() { return ''+ this.x +': '+ this.y +' '; }" })
                .SetPlotOptions(new PlotOptions
                {
                    Column = new PlotOptionsColumn
                    {
                        PointPadding = 0.1,
                        BorderWidth = 0
                        
                    }
                })
                .SetSeries(new[]
                {
                    new Series { Name = "100n",  Data = new Data(_obj100n) },
                    new Series { Name = "201u",  Data = new Data(_obj201u) },
                    new Series { Name = "300u",  Data = new Data(_obj300u) },
                    new Series { Name = "400u",  Data = new Data(_obj400u) }// Added Series


                });

            return View(chart);


        }


        //SEARCHING UR/SC RATIO
        [HttpPost]
        public ActionResult Search_URSC(FormCollection form)
        {
            ChartFilterModel objChartFilterModel = new ChartFilterModel();

            objChartFilterModel.SortCenter = form["SortCenter"].ToString();
            objChartFilterModel.Item = form["URSCItem"].ToString();
            if (form["RecDateFrom"].ToString() != string.Empty && form["RecDateTo"].ToString() != string.Empty)
            {
                objChartFilterModel.DateRecFrom = Convert.ToDateTime(form["RecDateFrom"].ToString());
                objChartFilterModel.DateRecTo = Convert.ToDateTime(form["RecDateTo"].ToString());
            }

            TempData["ChartFilterModel"] = objChartFilterModel;// Assign to Temp Data

            return RedirectToAction("URSC", "ChartData"); // Reload with Filter
        }



        //UR/SC  - Chart
        public ActionResult URSC()
        {


            var _ChartFiltermodel = TempData["ChartFilterModel"] as ChartFilterModel; // Getting the TempData ChartFilter Model


            var username = HttpContext.User.Identity.Name;

            var employee = _documentRepositoryAsync
                .GetRepository<Account>()
                .Query(a => a.Username == username)
                .Select(e => e.Employee)
                .SingleOrDefault();

            if (employee == null) return View();
            var viewModels = _documentRepositoryAsync
                 .Query(e => e.EmployeeId == employee.Id)
                .Select(d => new DocumentURSCModel()
                {
                    SortCenter = d.SortCenter,
                    DateRec = d.ReceivedDateFrom,
                    URSC_BL_Pallet = d.URSC_BL_Pallet,
                    URSC_BL_Composite = d.URSC_BL_Composite,
                    URSC_BL_HollowProfile = d.URSC_BL_HollowProfile,
                    URSC_BL_TopFrames = d.URSC_BL_TopFrames,
                    URSC_UR_Pallet = d.URSC_UR_Pallet,
                    URSC_UR_Composite = d.URSC_UR_Composite,
                    URSC_UR_HollowProfile = d.URSC_UR_HollowProfile,
                    URSC_UR_TopFrames = d.URSC_UR_TopFrames,
                    URSC_SC_Pallet = d.URSC_SC_Pallet,
                    URSC_SC_Composite = d.URSC_SC_Composite,
                    URSC_SC_HollowProfile = d.URSC_SC_HollowProfile,
                    URSC_SC_TopFrames = d.URSC_SC_TopFrames,
                })
                .ToList();

            // APPLY FILTERING

            if (_ChartFiltermodel != null)
            {
                if (_ChartFiltermodel.SortCenter != "All" && _ChartFiltermodel.DateRecFrom != Convert.ToDateTime("01/01/0001"))
                {
                    ViewBag.ReportParam = "  Filter: ( " + _ChartFiltermodel.SortCenter + " Sort Center & Received From : " + _ChartFiltermodel.DateRecFrom.ToShortDateString() + "  to  " + _ChartFiltermodel.DateRecTo.ToShortDateString() + " ) ";
                    viewModels = viewModels.Where(x => x.SortCenter == _ChartFiltermodel.SortCenter && x.DateRec >= _ChartFiltermodel.DateRecFrom && x.DateRec <= _ChartFiltermodel.DateRecTo).ToList();
                }
                else if (_ChartFiltermodel.SortCenter != "All" && _ChartFiltermodel.DateRecFrom == Convert.ToDateTime("01/01/0001"))
                {
                    ViewBag.ReportParam = "  Filter: ( " + _ChartFiltermodel.SortCenter + " Sort Center Data ) ";

                    viewModels = viewModels.Where(x => x.SortCenter == _ChartFiltermodel.SortCenter).ToList();// SORT CENTER ONLY  
                }

                else if (_ChartFiltermodel.SortCenter == "All" && _ChartFiltermodel.DateRecFrom != Convert.ToDateTime("01/01/0001"))
                {
                    ViewBag.ReportParam = "  Filter: ( Data from all Sort Centers  & Received From : " + _ChartFiltermodel.DateRecFrom.ToLongDateString() + "  to  " + _ChartFiltermodel.DateRecTo.ToLongDateString() + " ) ";

                    viewModels = viewModels.Where(x => x.DateRec >= _ChartFiltermodel.DateRecFrom && x.DateRec <= _ChartFiltermodel.DateRecTo).ToList(); // DATE RECEIVED FROM ONLY
                }
            }

            else
            {
                ChartFilterModel _URSCFilter = new ChartFilterModel();
                _URSCFilter.SortCenter = "DrakeWestEnd";
                _URSCFilter.Item = "Pallete";// Default to Pallete

                viewModels = viewModels.Where(x => x.SortCenter == _URSCFilter.SortCenter).ToList();
                _ChartFiltermodel = _URSCFilter;
            }


            string[] _strDateRec = new string[viewModels.Count];
            object[] _objDateRec = new object[viewModels.Count];
            
            //==PALETTE==
            object[] _objBL_Pallete = new object[viewModels.Count];
            object[] _objUR_Pallete = new object[viewModels.Count];
            object[] _objSC_Pallete = new object[viewModels.Count];

            //==COMPOSITE==
            object[] _objBL_Composite = new object[viewModels.Count];
            object[] _objUR_Composite = new object[viewModels.Count];
            object[] _objSC_Composite = new object[viewModels.Count];

            //==HOLLOW PROFILE==
            object[] _objBL_HollowProfile = new object[viewModels.Count];
            object[] _objUR_HollowProfile = new object[viewModels.Count];
            object[] _objSC_HollowProfile = new object[viewModels.Count];

            //==TOP FRAMES==
             object[] _objBL_TopFrames = new object[viewModels.Count];
             object[] _objUR_TopFrames = new object[viewModels.Count];
             object[] _objSC_TopFrames = new object[viewModels.Count];

             object[] _objBL_ = new object[viewModels.Count];
             object[] _objSC_ = new object[viewModels.Count];
             object[] _objUR_ = new object[viewModels.Count];



            int i = 0;

            foreach (DocumentURSCModel item in viewModels)
            {

                _objDateRec.SetValue(item.DateRec.ToString("d MMM"), i);

             
                if (_ChartFiltermodel.Item == "Pallete")
                {
                    _objBL_.SetValue(item.URSC_BL_Pallet,i);
                    _objUR_.SetValue(item.URSC_UR_Pallet, i);
                    _objSC_.SetValue(item.URSC_SC_Pallet, i);
                }
                if (_ChartFiltermodel.Item == "Composite") 
                {
                    _objBL_.SetValue(item.URSC_BL_Composite, i);
                    _objUR_.SetValue(item.URSC_UR_Composite, i);
                    _objSC_.SetValue(item.URSC_SC_Composite, i);

                }
                if (_ChartFiltermodel.Item == "HollowProfile")
                { 
                    _objBL_.SetValue(item.URSC_BL_HollowProfile, i);
                    _objUR_.SetValue(item.URSC_UR_HollowProfile, i);
                    _objSC_.SetValue(item.URSC_SC_HollowProfile, i);
           
                }
                if (_ChartFiltermodel.Item == "TopFrames") 
                { 
                    _objBL_.SetValue(item.URSC_BL_TopFrames, i);
                    _objUR_.SetValue(item.URSC_UR_TopFrames, i);
                    _objSC_.SetValue(item.URSC_SC_TopFrames, i);
           
                }
              


                _strDateRec[i] = string.Format(item.DateRec.ToString("d MMM"), i);

                i++;
            }



            Highcharts chart = new Highcharts("chart")
                //.InitChart(new Chart { DefaultSeriesType = ChartTypes.Column })
                 .InitChart(new Chart
                 {
                     Type = ChartTypes.Column,
                     Margin = new[] { 80 },
                     Options3d = new ChartOptions3d
                     {
                         Enabled = true,
                         Alpha = 10,
                         Beta = 0,
                         Depth = 75
                     }
                 })

                .SetTitle(new Title { Text = "OI - URSC/ Ratio " })
                //.SetSubtitle(new Subtitle { Text = "Source: Jeff@Drake.Com" })

                .SetXAxis(new XAxis { Categories = _strDateRec }) // The Date Received
                .SetYAxis(new YAxis
                {
                    Min = 0,
                    Title = new YAxisTitle { Text = "Items Sorted by Percentage" }
                })
                .SetLegend(new Legend
                {
                    Layout = Layouts.Vertical,
                    Align = HorizontalAligns.Left,
                    VerticalAlign = VerticalAligns.Top,
                    X = 50,
                    Y = 10,
                    Floating = true,
                    BackgroundColor = new BackColorOrGradient(ColorTranslator.FromHtml("#FFFFFF")),
                    Shadow = true
                })
                .SetTooltip(new Tooltip { Formatter = @"function() { return ''+ this.x +': '+ this.y +' %'; }" })
                .SetPlotOptions(new PlotOptions
                {
                    Column = new PlotOptionsColumn
                    {
                        PointPadding = 0.1,
                        BorderWidth = 0

                    }
                })
                .SetSeries(new[]
                {
                    new Series { Name = "BL/QI",  Data = new Data(_objBL_) },
                    new Series { Name = "UR/QI",  Data = new Data(_objUR_) },
                    new Series { Name = "SC/QI",  Data = new Data(_objSC_) },


                });

            return View(chart);


        }




        //SEARCHING TRUCK MOVEMEBT
        [HttpPost]
        public ActionResult Search_TruckMovement(FormCollection form)
        {
            ChartFilterModel objChartFilterModel = new ChartFilterModel();

            objChartFilterModel.SortCenter = form["SortCenter"].ToString();
            if (form["RecDateFrom"].ToString() != string.Empty && form["RecDateTo"].ToString() != string.Empty)
            {
                objChartFilterModel.DateRecFrom = Convert.ToDateTime(form["RecDateFrom"].ToString());
                objChartFilterModel.DateRecTo = Convert.ToDateTime(form["RecDateTo"].ToString());
            }

            TempData["ChartFilterModel"] = objChartFilterModel;// Assign to Temp Data

            return RedirectToAction("TruckMovement", "ChartData"); // Reload with Filter
        }



        //TRUCK MOVEMEBT  - Chart
        public ActionResult TruckMovement()
        {


            var _ChartFiltermodel = TempData["ChartFilterModel"] as ChartFilterModel; // Getting the TempData ChartFilter Model


            var username = HttpContext.User.Identity.Name;

            var employee = _documentRepositoryAsync
                .GetRepository<Account>()
                .Query(a => a.Username == username)
                .Select(e => e.Employee)
                .SingleOrDefault();

            if (employee == null) return View();
            var viewModels = _documentRepositoryAsync
                 .Query(e => e.EmployeeId == employee.Id)
                .Select(d => new DocumentTruckMovementModel()
                {
                    SortCenter = d.SortCenter,
                    DateRec = d.ReceivedDateFrom,
                    Truck_Movement_Ratio = d.Truck_Movement_Ratio,
                })
                .ToList();

            // APPLY FILTERING

            if (_ChartFiltermodel != null)
            {
                if (_ChartFiltermodel.SortCenter != "All" && _ChartFiltermodel.DateRecFrom != Convert.ToDateTime("01/01/0001"))
                {
                    ViewBag.ReportParam = "  Filter: ( " + _ChartFiltermodel.SortCenter + " Sort Center & Received From : " + _ChartFiltermodel.DateRecFrom.ToShortDateString() + "  to  " + _ChartFiltermodel.DateRecTo.ToShortDateString() + " ) ";
                    viewModels = viewModels.Where(x => x.SortCenter == _ChartFiltermodel.SortCenter && x.DateRec >= _ChartFiltermodel.DateRecFrom && x.DateRec <= _ChartFiltermodel.DateRecTo).ToList();
                }
                else if (_ChartFiltermodel.SortCenter != "All" && _ChartFiltermodel.DateRecFrom == Convert.ToDateTime("01/01/0001"))
                {
                    ViewBag.ReportParam = "  Filter: ( " + _ChartFiltermodel.SortCenter + " Sort Center Data ) ";

                    viewModels = viewModels.Where(x => x.SortCenter == _ChartFiltermodel.SortCenter).ToList();// SORT CENTER ONLY  
                }

                else if (_ChartFiltermodel.SortCenter == "All" && _ChartFiltermodel.DateRecFrom != Convert.ToDateTime("01/01/0001"))
                {
                    ViewBag.ReportParam = "  Filter: ( Data from all Sort Centers  & Received From : " + _ChartFiltermodel.DateRecFrom.ToLongDateString() + "  to  " + _ChartFiltermodel.DateRecTo.ToLongDateString() + " ) ";

                    viewModels = viewModels.Where(x => x.DateRec >= _ChartFiltermodel.DateRecFrom && x.DateRec <= _ChartFiltermodel.DateRecTo).ToList(); // DATE RECEIVED FROM ONLY
                }
            }

            

            string[] _strDateRec = new string[viewModels.Count];
            object[] _objDateRec = new object[viewModels.Count];

            //==TruckMovement Ratio==
            object[] _objTruckMovement = new object[viewModels.Count];
           


            int i = 0;

            foreach (DocumentTruckMovementModel item in viewModels)
            {

                _objDateRec.SetValue(item.DateRec.ToString("d MMM"), i);

                    _objTruckMovement.SetValue(item.Truck_Movement_Ratio, i);

                _strDateRec[i] = string.Format(item.DateRec.ToString("d MMM"), i);

                i++;
            }



            Highcharts chart = new Highcharts("chart")
                //.InitChart(new Chart { DefaultSeriesType = ChartTypes.Column })
                 .InitChart(new Chart
                 {
                     Type = ChartTypes.Column,
                     Margin = new[] { 80 },
                     Options3d = new ChartOptions3d
                     {
                         Enabled = true,
                         Alpha = 10,
                         Beta = 0,
                         Depth = 75
                     }
                 })

                .SetTitle(new Title { Text = "OI - Truck Movement Ratio " })
                //.SetSubtitle(new Subtitle { Text = "Source: Jeff@Drake.Com" })

                .SetXAxis(new XAxis { Categories = _strDateRec }) // The Date Received
                .SetYAxis(new YAxis
                {
                    Min = 0,
                    Title = new YAxisTitle { Text = "Items Sorted by Percentage" }
                })
                .SetLegend(new Legend
                {
                    Layout = Layouts.Vertical,
                    Align = HorizontalAligns.Left,
                    VerticalAlign = VerticalAligns.Top,
                    X = 50,
                    Y = 10,
                    Floating = true,
                    BackgroundColor = new BackColorOrGradient(ColorTranslator.FromHtml("#FFFFFF")),
                    Shadow = true
                })
                .SetTooltip(new Tooltip { Formatter = @"function() { return ''+ this.x +': '+ this.y +''; }" })
                .SetPlotOptions(new PlotOptions
                {
                    Column = new PlotOptionsColumn
                    {
                        PointPadding = 0.1,
                        BorderWidth = 0

                    }
                })
                .SetSeries(new[]
                {
                    new Series { Name = "Truck Movement Rate",  Data = new Data(_objTruckMovement) },
               


                });

            return View(chart);


        }



    }
}

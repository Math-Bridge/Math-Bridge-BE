using MathBridgeSystem.Application.DTOs.Statistics;
using MathBridgeSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace MathBridgeSystem.Api.Controllers
{
    [Route("api/statistics")]
    [ApiController]
    [Authorize(Roles = "admin,staff")]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;

        public StatisticsController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        }

        #region User Statistics

        /// <summary>
        /// Get overall user statistics
        /// </summary>
        /// <returns>User statistics including total, active, and breakdown by role</returns>
        [HttpGet("users/overview")]
        public async Task<IActionResult> GetUserStatistics()
        {
            try
            {
                var stats = await _statisticsService.GetUserStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving user statistics." });
            }
        }

        /// <summary>
        /// Get user registration trends over a period
        /// </summary>
        /// <param name="startDate">Start date for trend analysis</param>
        /// <param name="endDate">End date for trend analysis</param>
        /// <returns>User registration trends</returns>
        [HttpGet("users/registrations")]
        public async Task<IActionResult> GetUserRegistrationTrends(
            [FromQuery] [Required] DateTime startDate,
            [FromQuery] [Required] DateTime endDate)
        {
            if (startDate > endDate)
                return BadRequest(new { error = "Start date must be before end date." });

            try
            {
                var trends = await _statisticsService.GetUserRegistrationTrendsAsync(startDate, endDate);
                return Ok(trends);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving user registration trends." });
            }
        }

        /// <summary>
        /// Get user distribution by location
        /// </summary>
        /// <returns>User count by city</returns>
        [HttpGet("users/location")]
        public async Task<IActionResult> GetUserLocationDistribution()
        {
            try
            {
                var distribution = await _statisticsService.GetUserLocationDistributionAsync();
                return Ok(distribution);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving user location statistics." });
            }
        }

        /// <summary>
        /// Get wallet balance statistics for parents
        /// </summary>
        /// <returns>Wallet statistics including totals, averages, and distribution</returns>
        [HttpGet("users/wallet")]
        public async Task<IActionResult> GetWalletStatistics()
        {
            try
            {
                var stats = await _statisticsService.GetWalletStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving wallet statistics." });
            }
        }

        #endregion

        #region Session Statistics

        /// <summary>
        /// Get overall session statistics
        /// </summary>
        /// <returns>Session counts by status and completion rate</returns>
        [HttpGet("sessions/overview")]
        public async Task<IActionResult> GetSessionStatistics()
        {
            try
            {
                var stats = await _statisticsService.GetSessionStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving session statistics." });
            }
        }

        /// <summary>
        /// Get online vs offline session comparison
        /// </summary>
        /// <returns>Comparison of online and offline sessions with percentages</returns>
        [HttpGet("sessions/online-vs-offline")]
        public async Task<IActionResult> GetSessionOnlineVsOffline()
        {
            try
            {
                var stats = await _statisticsService.GetSessionOnlineVsOfflineAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving session comparison." });
            }
        }

        /// <summary>
        /// Get session trends over a period
        /// </summary>
        /// <param name="startDate">Start date for trend analysis</param>
        /// <param name="endDate">End date for trend analysis</param>
        /// <returns>Session trends by date</returns>
        [HttpGet("sessions/trends")]
        public async Task<IActionResult> GetSessionTrends(
            [FromQuery] [Required] DateTime startDate,
            [FromQuery] [Required] DateTime endDate)
        {
            if (startDate > endDate)
                return BadRequest(new { error = "Start date must be before end date." });

            try
            {
                var trends = await _statisticsService.GetSessionTrendsAsync(startDate, endDate);
                return Ok(trends);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving session trends." });
            }
        }

        #endregion

        #region Tutor Statistics

        /// <summary>
        /// Get overall tutor statistics
        /// </summary>
        /// <returns>Total tutors, average rating, and breakdown by feedback</returns>
        [HttpGet("tutors/overview")]
        public async Task<IActionResult> GetTutorStatistics()
        {
            try
            {
                var stats = await _statisticsService.GetTutorStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving tutor statistics." });
            }
        }

        /// <summary>
        /// Get top-rated tutors
        /// </summary>
        /// <param name="limit">Number of top tutors to return (default: 10)</param>
        /// <returns>List of top-rated tutors with their ratings</returns>
        [HttpGet("tutors/top-rated")]
        public async Task<IActionResult> GetTopRatedTutors([FromQuery] int limit = 10)
        {
            if (limit < 1 || limit > 100)
                return BadRequest(new { error = "Limit must be between 1 and 100." });

            try
            {
                var tutors = await _statisticsService.GetTopRatedTutorsAsync(limit);
                return Ok(tutors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving top-rated tutors." });
            }
        }

        /// <summary>
        /// Get most active tutors by session count
        /// </summary>
        /// <param name="limit">Number of top tutors to return (default: 10)</param>
        /// <returns>List of most active tutors with session counts</returns>
        [HttpGet("tutors/most-active")]
        public async Task<IActionResult> GetMostActiveTutors([FromQuery] int limit = 10)
        {
            if (limit < 1 || limit > 100)
                return BadRequest(new { error = "Limit must be between 1 and 100." });

            try
            {
                var tutors = await _statisticsService.GetMostActiveTutorsAsync(limit);
                return Ok(tutors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving most active tutors." });
            }
        }

        #endregion

        #region Financial Statistics

        /// <summary>
        /// Get overall revenue statistics
        /// </summary>
        /// <returns>Total revenue, transaction counts, and success rate</returns>
        [HttpGet("financial/revenue")]
        public async Task<IActionResult> GetRevenueStatistics()
        {
            try
            {
                var stats = await _statisticsService.GetRevenueStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving revenue statistics." });
            }
        }

        /// <summary>
        /// Get revenue trends over a period
        /// </summary>
        /// <param name="startDate">Start date for trend analysis</param>
        /// <param name="endDate">End date for trend analysis</param>
        /// <returns>Revenue trends by date</returns>
        [HttpGet("financial/revenue-trends")]
        public async Task<IActionResult> GetRevenueTrends(
            [FromQuery] [Required] DateTime startDate,
            [FromQuery] [Required] DateTime endDate)
        {
            if (startDate > endDate)
                return BadRequest(new { error = "Start date must be before end date." });

            try
            {
                var trends = await _statisticsService.GetRevenueTrendsAsync(startDate, endDate);
                return Ok(trends);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving revenue trends." });
            }
        }

        #endregion
    }
}


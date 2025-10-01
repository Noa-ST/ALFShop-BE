using eCommerceApp.Aplication.Services.Interfaces.Logging;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;

namespace eCommerceApp.Infrastructure.Midleware
{
    /// <summary>
    /// Middleware xử lý ngoại lệ tập trung cho ứng dụng ASP.NET Core.
    ///
    /// Mục đích:
    /// - Bắt các lỗi liên quan đến cơ sở dữ liệu (DbUpdateException).
    /// - Trả về mã lỗi HTTP phù hợp và thông điệp dễ hiểu cho phía client.
    ///
    /// Các lỗi được xử lý cụ thể:
    /// - SqlException 2627: Vi phạm ràng buộc UNIQUE (trùng khóa).
    /// - SqlException 515 : Chèn dữ liệu thiếu giá trị NOT NULL.
    /// - SqlException 547 : Vi phạm ràng buộc FOREIGN KEY.
    /// - Lỗi khác: Trả về mã 500 với thông báo chung.
    ///
    /// Cách sử dụng:
    /// Đăng ký middleware này trong Program.cs hoặc Startup.cs:
    ///     app.UseMiddleware<ExceptionHandlingMiddleware>();
    ///
    /// Lưu ý:
    /// - Đảm bảo middleware này được đặt trước UseEndpoints.
    /// - Có thể mở rộng thêm các loại exception khác nếu cần.
    /// </summary>

    public class ExceptionHandlingMiddleware(RequestDelegate _next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DbUpdateException ex)
            {
                var logger = context.RequestServices.GetRequiredService<IAppLogger<ExceptionHandlingMiddleware>>();
                context.Response.ContentType = "application/json";
                if (ex.InnerException is SqlException innerException)
                {
                    logger.LogError(innerException, "Sql Exception");
                    switch (innerException.Number)
                    {
                        case 2627: // Unique constraint violation
                            context.Response.StatusCode = StatusCodes.Status409Conflict;
                            await context.Response.WriteAsync("Unique constraint violation");
                            break;
                        case 515: // Cannot insert null
                            context.Response.StatusCode = StatusCodes.Status400BadRequest;
                            await context.Response.WriteAsync("Cannot insert null");
                            break;
                        case 547: // Foreign key constraint violation
                            context.Response.StatusCode = StatusCodes.Status409Conflict;
                            await context.Response.WriteAsync("Foreign key constraint violation");
                            break;
                        default:
                            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                            await context.Response.WriteAsync("An error occurred while processing your request.");
                            break;
                    }
                }

                else
                {
                    logger.LogError(ex, "Relate EFcore Exception");
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("An error occurred while saving the entity changes.");
                }
            }
            catch (Exception ex)
            {
                var logger = context.RequestServices.GetRequiredService<IAppLogger<ExceptionHandlingMiddleware>>();
                logger.LogError(ex, "Unknown Exception");
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsync("An error occurred: " + ex.Message);
            }
        }
    }
}

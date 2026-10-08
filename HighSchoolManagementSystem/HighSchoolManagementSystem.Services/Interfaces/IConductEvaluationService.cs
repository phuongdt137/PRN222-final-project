using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.DTOs;

namespace HighSchoolManagementSystem.Services.Interfaces;

public interface IConductEvaluationService
{
    /// <summary>
    /// Lấy danh sách và thống kê đánh giá hạnh kiểm của học sinh trong lớp (GVCN, Hiệu trưởng, Giáo vụ).
    /// </summary>
    Task<ServiceResult<ClassConductSummaryDto>> GetClassConductSummaryAsync(int classId, int semesterId, int? currentUserId, string userRole);

    /// <summary>
    /// Đánh giá hoặc cập nhật xếp loại và nhận xét hạnh kiểm cho 1 học sinh (Chỉ GVCN).
    /// </summary>
    Task<ServiceResult<ConductEvaluationItemDto>> SaveStudentConductAsync(SaveStudentConductDto dto, int currentUserId, string userRole);

    /// <summary>
    /// Đánh giá hoặc cập nhật hạnh kiểm hàng loạt cho cả lớp (Chỉ GVCN).
    /// </summary>
    Task<ServiceResult<int>> BulkSaveConductAsync(BulkSaveConductDto dto, int currentUserId, string userRole);

    /// <summary>
    /// Tra cứu chi tiết hạnh kiểm học kỳ của học sinh (Dành cho Học sinh, Phụ huynh, GVCN, Hiệu trưởng).
    /// </summary>
    Task<ServiceResult<StudentConductDetailDto>> GetStudentConductAsync(int studentId, int semesterId, int currentUserId, string userRole);

    /// <summary>
    /// Tra cứu lịch sử rèn luyện và hạnh kiểm qua các học kỳ của học sinh.
    /// </summary>
    Task<ServiceResult<IEnumerable<StudentConductHistoryItemDto>>> GetStudentConductHistoryAsync(int studentId, int currentUserId, string userRole);

    /// <summary>
    /// Báo cáo tổng quan tình hình đánh giá hạnh kiểm toàn trường (Dành cho Hiệu trưởng, Giáo vụ).
    /// </summary>
    Task<ServiceResult<PrincipalConductOverviewDto>> GetPrincipalOverviewAsync(int semesterId);
}

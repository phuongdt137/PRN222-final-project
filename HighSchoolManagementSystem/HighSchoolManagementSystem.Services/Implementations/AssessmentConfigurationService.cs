using HighSchoolManagementSystem.Models.Entities;
using HighSchoolManagementSystem.Repositories.Interfaces;
using HighSchoolManagementSystem.Services.Common;
using HighSchoolManagementSystem.Services.Interfaces;

namespace HighSchoolManagementSystem.Services.Implementations;

public class AssessmentConfigurationService(IAssessmentRepository repository) : IAssessmentConfigurationService
{
    public async Task<ServiceResult<IReadOnlyList<AssessmentType>>> GetAllAsync()
    {
        try { return ServiceResult<IReadOnlyList<AssessmentType>>.Ok((await repository.GetAllAssessmentTypesAsync()).ToList()); }
        catch { return ServiceResult<IReadOnlyList<AssessmentType>>.Fail(ErrorCodes.DatabaseError, "Không thể tải cấu hình đánh giá."); }
    }

    public async Task<ServiceResult<AssessmentType>> GetByIdAsync(int id)
    {
        try
        {
            var item = await repository.GetAssessmentTypeByIdAsync(id);
            return item == null
                ? ServiceResult<AssessmentType>.Fail(ErrorCodes.ResourceNotFound, "Không tìm thấy loại đánh giá.")
                : ServiceResult<AssessmentType>.Ok(item);
        }
        catch { return ServiceResult<AssessmentType>.Fail(ErrorCodes.DatabaseError, "Không thể tải loại đánh giá."); }
    }

    public async Task<ServiceResult<AssessmentType>> SaveAsync(int? id, string typeCode, string typeName, decimal weight,
        int minEntries, int maxEntries, decimal minScore, decimal maxScore)
    {
        if (string.IsNullOrWhiteSpace(typeCode) || string.IsNullOrWhiteSpace(typeName))
            return ServiceResult<AssessmentType>.Fail(ErrorCodes.ValidationFailed, "Mã và tên loại đánh giá không được để trống.");
        if (minEntries < 0 || maxEntries < 0 || minEntries > maxEntries)
            return ServiceResult<AssessmentType>.Fail(ErrorCodes.ValidationFailed, "Số lần tối thiểu không được lớn hơn tối đa.");
        if (weight <= 0 || weight > 100)
            return ServiceResult<AssessmentType>.Fail(ErrorCodes.ValidationFailed, "Trọng số phải lớn hơn 0% và không vượt quá 100%.");
        if (minScore < 0 || maxScore > 10 || minScore > maxScore)
            return ServiceResult<AssessmentType>.Fail(ErrorCodes.ValidationFailed, "Khoảng điểm phải nằm trong 0 đến 10 và giá trị tối thiểu không được lớn hơn tối đa.");

        var code = typeCode.Trim().ToUpperInvariant();
        try
        {
            if (await repository.AssessmentTypeCodeExistsAsync(code, id))
                return ServiceResult<AssessmentType>.Fail(ErrorCodes.ResourceAlreadyExists, "Mã loại đánh giá đã tồn tại.");
            if (await repository.GetAssessmentTypeWeightTotalAsync(id) + weight > 100)
                return ServiceResult<AssessmentType>.Fail(ErrorCodes.ValidationFailed, "Tổng trọng số không được vượt quá 100%.");

            AssessmentType item;
            if (id.HasValue)
            {
                var existing = await repository.GetAssessmentTypeByIdAsync(id.Value);
                if (existing == null)
                    return ServiceResult<AssessmentType>.Fail(ErrorCodes.ResourceNotFound, "Không tìm thấy loại đánh giá cần cập nhật.");
                if (existing.Assessments.Count > maxEntries)
                    return ServiceResult<AssessmentType>.Fail(ErrorCodes.ValidationFailed,
                        $"Không thể đặt số lần tối đa thấp hơn số bài đã tồn tại ({existing.Assessments.Count}).");
                item = existing;
            }
            else
            {
                item = new AssessmentType();
                await repository.AddAssessmentTypeAsync(item);
            }

            item.TypeCode = code;
            item.TypeName = typeName.Trim();
            item.Weight = weight;
            item.MinEntries = minEntries;
            item.MaxEntries = maxEntries;
            item.MinScore = minScore;
            item.MaxScore = maxScore;
            if (id.HasValue) await repository.UpdateAssessmentTypeAsync(item);
            await repository.SaveChangesAsync();
            return ServiceResult<AssessmentType>.Ok(item, "Đã lưu cấu hình đánh giá.");
        }
        catch { return ServiceResult<AssessmentType>.Fail(ErrorCodes.DatabaseError, "Không thể lưu cấu hình đánh giá."); }
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        try
        {
            var item = await repository.GetAssessmentTypeByIdAsync(id);
            if (item == null) return ServiceResult.Fail(ErrorCodes.ResourceNotFound, "Không tìm thấy loại đánh giá.");
            if (item.Assessments.Count > 0) return ServiceResult.Fail(ErrorCodes.OperationFailed, "Không thể xóa loại đánh giá đã được sử dụng.");
            await repository.DeleteAssessmentTypeAsync(item);
            await repository.SaveChangesAsync();
            return ServiceResult.Ok("Đã xóa loại đánh giá.");
        }
        catch { return ServiceResult.Fail(ErrorCodes.DatabaseError, "Không thể xóa loại đánh giá."); }
    }
}

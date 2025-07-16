using App.BLL.Interfaces;
using App.Commons.ResponseModel;
using App.DAL.Queries.Implementations;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.Handbag;
using App.Entities.Entities_2;
using Microsoft.Extensions.Logging;

namespace App.BLL.Implementations;

public class HandbagService : IHandbagService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HandbagService> _logger;

    public HandbagService(IUnitOfWork unitOfWork, ILogger<HandbagService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<BaseResponseModel<List<HandbagResponseDTO>>> GetAllHandbagsAsync()
    {
        try
        {
            var handbagRepo = _unitOfWork.GetRepo<Handbag>();

            var handbags = await handbagRepo.GetAllAsync(new AdvancedQueryBuilder<Handbag>()
                .Include(h => h.Brand)
                .Build());

            var result = handbags.Select(h => new HandbagResponseDTO(h)).ToList();

            return new BaseResponseModel<List<HandbagResponseDTO>>
            {
                IsSuccess = true,
                Data = result
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all handbags");
            return new BaseResponseModel<List<HandbagResponseDTO>>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving handbags."
            };
        }
    }

    public async Task<BaseResponseModel<HandbagResponseDTO>> GetHandbagByIdAsync(int id)
    {
        try
        {
            var handbagRepo = _unitOfWork.GetRepo<Handbag>();

            var handbag = await handbagRepo.GetSingleAsync(new AdvancedQueryBuilder<Handbag>()
                .WithPredicate(h => h.HandbagID == id)
                .Include(h => h.Brand)
                .Build());

            if (handbag == null)
            {
                return new BaseResponseModel<HandbagResponseDTO>
                {
                    IsSuccess = false,
                    Message = "Handbag not found."
                };
            }

            return new BaseResponseModel<HandbagResponseDTO>
            {
                IsSuccess = true,
                Data = new HandbagResponseDTO(handbag)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving handbag with ID {HandbagId}", id);
            return new BaseResponseModel<HandbagResponseDTO>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving the handbag."
            };
        }
    }

    public async Task<BaseResponseModel<HandbagResponseDTO>> CreateHandbagAsync(CreateHandbagDTO createHandbagDTO)
    {
        try
        {
            var handbagRepo = _unitOfWork.GetRepo<Handbag>();
            var brandRepo = _unitOfWork.GetRepo<Brand>();

            // Check if brand exists
            var brand = await brandRepo.GetSingleAsync(new QueryBuilder<Brand>()
                .WithPredicate(b => b.BrandID == createHandbagDTO.BrandId)
                .Build());

            if (brand == null)
            {
                return new BaseResponseModel<HandbagResponseDTO>
                {
                    IsSuccess = false,
                    Message = "Brand not found."
                };
            }

            // Generate new HandbagID
            var maxId = await handbagRepo.GetAllAsync(new QueryBuilder<Handbag>().Build());
            var newId = maxId.Any() ? maxId.Max(h => h.HandbagID) + 1 : 1;

            var handbag = new Handbag
            {
                HandbagID = newId,
                BrandID = createHandbagDTO.BrandId,
                ModelName = createHandbagDTO.ModelName,
                Material = createHandbagDTO.Material,
                Color = createHandbagDTO.Color,
                Price = createHandbagDTO.Price,
                Stock = createHandbagDTO.Stock,
                ReleaseDate = createHandbagDTO.ReleaseDate
            };

            await handbagRepo.CreateAsync(handbag);
            await _unitOfWork.SaveChangesAsync();

            // Get the created handbag with brand info
            var createdHandbag = await handbagRepo.GetSingleAsync(new AdvancedQueryBuilder<Handbag>()
                .WithPredicate(h => h.HandbagID == newId)
                .Include(h => h.Brand)
                .Build());

            return new BaseResponseModel<HandbagResponseDTO>
            {
                IsSuccess = true,
                Message = "Handbag created successfully.",
                Data = new HandbagResponseDTO(createdHandbag!)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating handbag");
            return new BaseResponseModel<HandbagResponseDTO>
            {
                IsSuccess = false,
                Message = "An error occurred while creating the handbag."
            };
        }
    }

    public async Task<BaseResponseModel<HandbagResponseDTO>> UpdateHandbagAsync(int id, UpdateHandbagDTO updateHandbagDTO)
    {
        try
        {
            var handbagRepo = _unitOfWork.GetRepo<Handbag>();
            var brandRepo = _unitOfWork.GetRepo<Brand>();

            var handbag = await handbagRepo.GetSingleAsync(new QueryBuilder<Handbag>()
                .WithPredicate(h => h.HandbagID == id)
                .Build());

            if (handbag == null)
            {
                return new BaseResponseModel<HandbagResponseDTO>
                {
                    IsSuccess = false,
                    Message = "Handbag not found."
                };
            }

            // Check if brand exists if BrandId is provided
            if (updateHandbagDTO.BrandId.HasValue)
            {
                var brand = await brandRepo.GetSingleAsync(new QueryBuilder<Brand>()
                    .WithPredicate(b => b.BrandID == updateHandbagDTO.BrandId.Value)
                    .Build());

                if (brand == null)
                {
                    return new BaseResponseModel<HandbagResponseDTO>
                    {
                        IsSuccess = false,
                        Message = "Brand not found."
                    };
                }
                handbag.BrandID = updateHandbagDTO.BrandId.Value;
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(updateHandbagDTO.ModelName))
                handbag.ModelName = updateHandbagDTO.ModelName;

            if (!string.IsNullOrEmpty(updateHandbagDTO.Material))
                handbag.Material = updateHandbagDTO.Material;

            if (!string.IsNullOrEmpty(updateHandbagDTO.Color))
                handbag.Color = updateHandbagDTO.Color;

            if (updateHandbagDTO.Price.HasValue)
                handbag.Price = updateHandbagDTO.Price.Value;

            if (updateHandbagDTO.Stock.HasValue)
                handbag.Stock = updateHandbagDTO.Stock.Value;

            if (updateHandbagDTO.ReleaseDate.HasValue)
                handbag.ReleaseDate = updateHandbagDTO.ReleaseDate.Value;

            await handbagRepo.UpdateAsync(handbag);
            await _unitOfWork.SaveChangesAsync();

            // Get updated handbag with brand info
            var updatedHandbag = await handbagRepo.GetSingleAsync(new AdvancedQueryBuilder<Handbag>()
                .WithPredicate(h => h.HandbagID == id)
                .Include(h => h.Brand)
                .Build());

            return new BaseResponseModel<HandbagResponseDTO>
            {
                IsSuccess = true,
                Message = "Handbag updated successfully.",
                Data = new HandbagResponseDTO(updatedHandbag!)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating handbag with ID {HandbagId}", id);
            return new BaseResponseModel<HandbagResponseDTO>
            {
                IsSuccess = false,
                Message = "An error occurred while updating the handbag."
            };
        }
    }

    public async Task<BaseResponseModel> DeleteHandbagAsync(int id)
    {
        try
        {
            var handbagRepo = _unitOfWork.GetRepo<Handbag>();

            var handbag = await handbagRepo.GetSingleAsync(new QueryBuilder<Handbag>()
                .WithPredicate(h => h.HandbagID == id)
                .Build());

            if (handbag == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    Message = "Handbag not found."
                };
            }

            await handbagRepo.DeleteAsync(handbag);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel
            {
                IsSuccess = true,
                Message = "Handbag deleted successfully."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting handbag with ID {HandbagId}", id);
            return new BaseResponseModel
            {
                IsSuccess = false,
                Message = "An error occurred while deleting the handbag."
            };
        }
    }

    public async Task<BaseResponseModel<List<SearchHandbagResultDTO>>> SearchHandbagsAsync(string? modelName, string? material)
    {
        try
        {
            var handbagRepo = _unitOfWork.GetRepo<Handbag>();

            var queryBuilder = new AdvancedQueryBuilder<Handbag>()
                .Include(h => h.Brand);

            // Build search predicate
            if (!string.IsNullOrEmpty(modelName) && !string.IsNullOrEmpty(material))
            {
                queryBuilder.WithPredicate(h =>
                    h.ModelName.Contains(modelName) &&
                    h.Material.Contains(material));
            }
            else if (!string.IsNullOrEmpty(modelName))
            {
                queryBuilder.WithPredicate(h => h.ModelName.Contains(modelName));
            }
            else if (!string.IsNullOrEmpty(material))
            {
                queryBuilder.WithPredicate(h => h.Material.Contains(material));
            }

            var handbags = await handbagRepo.GetAllAsync(queryBuilder.Build());

            // Group by brand name
            var groupedResults = handbags
                .GroupBy(h => h.Brand?.BrandName ?? "Unknown Brand")
                .Select(g => new SearchHandbagResultDTO
                {
                    BrandName = g.Key,
                    Handbags = g.Select(h => new HandbagResponseDTO(h)).ToList()
                })
                .ToList();

            return new BaseResponseModel<List<SearchHandbagResultDTO>>
            {
                IsSuccess = true,
                Data = groupedResults
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching handbags");
            return new BaseResponseModel<List<SearchHandbagResultDTO>>
            {
                IsSuccess = false,
                Message = "An error occurred while searching handbags."
            };
        }
    }

    public IQueryable<HandbagODataDTO> GetHandbagsQueryable()
    {
        try
        {
            var handbagRepo = _unitOfWork.GetRepo<Handbag>();

            var handbags = handbagRepo.Get(new AdvancedQueryBuilder<Handbag>()
                .Include(h => h.Brand)
                .WithTracking(false)
                .Build());

            return handbags.Select(h => new HandbagODataDTO
            {
                HandbagID = h.HandbagID,
                ModelName = h.ModelName,
                Material = h.Material,
                Color = h.Color,
                Price = h.Price,
                Stock = h.Stock,
                ReleaseDate = h.ReleaseDate,
                BrandID = h.BrandID,
                Brand = h.Brand != null ? new BrandODataDTO
                {
                    BrandID = h.Brand.BrandID,
                    BrandName = h.Brand.BrandName,
                    Country = h.Brand.Country,
                    FoundedYear = h.Brand.FoundedYear,
                    Website = h.Brand.Website
                } : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting handbags queryable for OData");
            return Enumerable.Empty<HandbagODataDTO>().AsQueryable();
        }
    }
}

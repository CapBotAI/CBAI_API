using System;
using App.Commons.ResponseModel;

namespace App.Commons.Interfaces;

public interface IValiationPipeline
{
    BaseResponseModel Validate();
}

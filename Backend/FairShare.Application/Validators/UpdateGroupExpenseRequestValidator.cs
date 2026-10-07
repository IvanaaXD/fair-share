using FairShare.Application.DTOs.GroupExpenses;
using FluentValidation;

namespace FairShare.Application.Validators
{
    /// <summary>
    /// An edited group expense must satisfy the same rules as a new one, so all rules of
    /// CreateGroupExpenseRequestValidator are included instead of being written twice.
    /// </summary>
    public class UpdateGroupExpenseRequestValidator : AbstractValidator<UpdateGroupExpenseRequest>
    {
        public UpdateGroupExpenseRequestValidator()
        {
            Include(new CreateGroupExpenseRequestValidator());
        }
    }
}

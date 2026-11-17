using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CargoFlow.Data;
using CargoFlow.Domain;
using CargoFlow.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CargoFlow.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/register/customer", RegisterCustomer);
        group.MapPost("/register/company", RegisterCompany);
        group.MapPost("/login", Login);
        return group;
    }

    private static async Task<Results<ProblemHttpResult, Ok<RegisterResponseDto>>> RegisterCustomer(
        UserManager<IdentityUser> userManager,
        RegisterCustomerRequestDto dto,
        CargoFlowDbContext db
    )
    {
        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {

            var user = new IdentityUser()
            {
                Email = dto.Email,
                UserName = dto.Email
            };

            var createUserResult = await userManager.CreateAsync(user, dto.Password);

            if (!createUserResult.Succeeded)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "User Creation Issue",
                    detail: "Unable to create user",
                    extensions: new Dictionary<string, object?>()
                    {
                        ["errors"] = createUserResult.Errors
                    }
                );
            }

            var addToRoleResult = await userManager.AddToRoleAsync(user, "Customer");
            if (!addToRoleResult.Succeeded)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "User Creation Issue",
                    detail: "Unable to create user",
                    extensions: new Dictionary<string, object?>()
                    {
                        ["errors"] = addToRoleResult.Errors
                    }
               );
            }

            var customer = new Customer()
            {
                Name = dto.Name,
                NationalId = dto.NationalId,
                CustomerUserId = user.Id
            };

            db.Customers.Add(customer);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return TypedResults.Ok(new RegisterResponseDto("User created successfully"));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "User Creation Issue",
                detail: "Unable to create user"
            );
        }
    }

    private static async Task<Results<ProblemHttpResult, Ok<RegisterResponseDto>>> RegisterCompany(
        UserManager<IdentityUser> userManager,
        CargoFlowDbContext db,
        RegisterCompanyRequestDto dto
    )
    {
        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {

            var user = new IdentityUser()
            {
                Email = dto.Email,
                UserName = dto.Email
            };

            var createUserResult = await userManager.CreateAsync(user, dto.Password);

            if (!createUserResult.Succeeded)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "User Creation Issue",
                    detail: "Unable to create user",
                    extensions: new Dictionary<string, object?>()
                    {
                        ["errors"] = createUserResult.Errors
                    }
                );
            }

            var addToRoleResult = await userManager.AddToRoleAsync(user, "Company");
            if (!addToRoleResult.Succeeded)
            {
                return TypedResults.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "User Creation Issue",
                    detail: "Unable to create user",

                    extensions: new Dictionary<string, object?>()
                    {
                        ["errors"] = addToRoleResult.Errors
                    }

               );
            }

            var company = new Company()
            {
                Email = dto.Email,
                Name = dto.Name,
                CompanyUserId = user.Id,
                TaxID = dto.TaxID
            };

            db.Companies.Add(company);
            await db.SaveChangesAsync();

            await transaction.CommitAsync();

            return TypedResults.Ok(new RegisterResponseDto("User created successfully"));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "User Creation Issue",
                detail: "Unable to create user 'SaveResult'"
           );
        }


    }

    private static async Task<Results<ProblemHttpResult, Ok<LoginResponseDto>>> Login(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IConfiguration configuration,
        CargoFlowDbContext db,
        LoginRequestDto dto
    )
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Invalid Credentials"
            );
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, dto.Password, false);

        if (!result.Succeeded)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Invalid Credentials"
            );
        }

        var isCompany = await db.Companies.AnyAsync(c => c.CompanyUserId == user.Id);
        var id = 0;
        if (isCompany)
        {
            id = db.Companies.Single(c => c.Id == id).Id;
        }
        else
        {
            id = db.Customers.Single(c => c.Id == id).Id;
        }

        var idClaim = isCompany ? new Claim("company_id", id.ToString()) : new Claim("customer_id", id.ToString());

        List<Claim> claims = [
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.NameIdentifier, user.Id),
            new ("user_type", isCompany ? "Company" : "Customer"),
            idClaim
        ];

        var roles = await userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Audience = jwtSettings["Audience"],
            Issuer = jwtSettings["Issuer"],
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryMinutes"]!)),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(securityToken);

        return TypedResults.Ok(new LoginResponseDto(jwtToken));
    }
}

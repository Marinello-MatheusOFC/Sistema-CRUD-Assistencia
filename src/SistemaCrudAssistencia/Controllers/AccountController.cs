using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaCrudAssistencia.Models.Entities;
using SistemaCrudAssistencia.Models.ViewModels;

namespace SistemaCrudAssistencia.Controllers;

/// <summary>
/// Autenticação e páginas auxiliares de conta.
/// Todo o conteúdo desta controller é anônimo (login e erros);
/// as demais áreas do sistema são protegidas pela FallbackPolicy do Program.cs.
/// </summary>
public class AccountController : Controller
{
    private readonly SignInManager<Usuario> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(SignInManager<Usuario> signInManager, ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel modelo, string? returnUrl = null)
    {
        modelo.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return View(modelo);

        var resultado = await _signInManager.PasswordSignInAsync(
            modelo.Email,
            modelo.Senha,
            modelo.ManterConectado,
            lockoutOnFailure: true);

        if (resultado.Succeeded)
        {
            _logger.LogInformation("Usuário autenticado: {Email}.", modelo.Email);
            return RedirecionarParaAreaProtegida(returnUrl);
        }

        if (resultado.IsLockedOut)
        {
            _logger.LogWarning("Conta bloqueada por excesso de tentativas: {Email}.", modelo.Email);
            ModelState.AddModelError(string.Empty,
                "Conta temporariamente bloqueada por excesso de tentativas de acesso. Tente novamente em alguns minutos.");
            return View(modelo);
        }

        _logger.LogWarning("Tentativa de autenticação inválida: {Email}.", modelo.Email);
        ModelState.AddModelError(string.Empty, "E-mail/usuário ou senha inválidos.");
        return View(modelo);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Sessão finalizada pelo usuário.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AcessoNegado() => View();

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult HttpStatus(int? codigo) =>
        View(StatusHttpViewModel.Criar(codigo));

    private IActionResult RedirecionarParaAreaProtegida(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToAction("Index", "Home");
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ultimo.Models;
using Ultimo.Services;
using static System.Net.Mime.MediaTypeNames;
using System.Security.Cryptography;

namespace Ultimo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartaoController : ControllerBase
    {
        private readonly CartoesService _cartoesService;
        //Extras extras;

        public CartaoController(CartoesService cartoesService)
        {
            _cartoesService = cartoesService;
        }

        private bool ChecarSenha(Cartao newCartao)
        {
            var tudoJunto = "" + (newCartao.DataNasc.Value.Day) + "" +
                (newCartao.DataNasc.Value.Month.ToString().Length == 1 ? ("0" + newCartao.DataNasc.Value.Month) : (newCartao.DataNasc.Value.Month))
                + "" + (newCartao.DataNasc.Value.Year.ToString().Substring(2));

            var idade = DateTime.Today.Year - newCartao.DataNasc.Value.Year;
            var senha = newCartao.Senha.ToString();

            if (idade >= 18 && senha.Length == 6 && senha != tudoJunto && int.TryParse(senha, out int n) && senha.Distinct().Count() == 6 && newCartao.Senha == newCartao.SenhaConfirm)
            {
                bool sequencia = false;
                for (int i = 0; i < senha.Length - 1; i++)
                {
                    if (senha[i] + 1 == senha[i + 1])
                    {
                        sequencia = true;
                    }
                }
                return sequencia? false : true;
            }
                
            return false;
        }

        [HttpGet]
        public async Task<List<Cartao>> Get() => await _cartoesService.GetAsync();

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Cartao>> Get(string id)
        {
            var cartao = await _cartoesService.GetAsync(id);

            if (cartao is null)
            {
                return NotFound();
            }

            return cartao;
        }

        [HttpPost("SolicitarCartão")]
        public async Task<IActionResult> Post(Cartao newCartao)
        {
            Random r = new Random();
            var senha_ok = ChecarSenha(newCartao);
            string aviso = null;

            if(DateTime.Today.Year - newCartao.DataNasc.Value.Year < 18)
                return BadRequest("É obrigatório ter 18 ou mais de idade para solicitar um cartão.\n");

            if (newCartao.Bandeira != "Mastercard" && newCartao.Bandeira != "Visa")
                aviso += "É necessário que a Bandeira seja 'Mastercard' ou 'Visa'.\n";

            if (newCartao.DataVenc != "5" && newCartao.DataVenc != "10" && newCartao.DataVenc != "15" && newCartao.DataVenc != "20")
                aviso += "É necessário que a Data de Vencimento seja '5', '10', '15' ou '20'.\n";

            if (newCartao.Tipo != "PLATINUM" && newCartao.Tipo != "GOLD" && newCartao.Tipo != "BLACK" && newCartao.Tipo != "DIAMOND")
            {
                aviso += "É necessário que o Tipo seja 'PLATINUM', 'GOLD', 'BLACK' ou 'DIAMOND'.\n";
            }
            else
            {
                switch(newCartao.Tipo)
                {
                    case "GOLD":
                        newCartao.Limite = "1500";
                        break;
                    case "PLATINUM":
                        newCartao.Limite = "15000";
                        break;
                    case "BLACK":
                        newCartao.Limite = "30000";
                        break;
                    case "DIAMOND":
                        newCartao.Limite = "SEM LIMITE";
                        break;
                }
            }

            if (!senha_ok)
            {
                aviso += "É necessário que a senha tenha 6 dígitos que não correspondam à data de nascimento, sem números repetidos ou sequenciais.\nConfirme se o campo de senha e confirmação de senha são iguais.\n";
                return BadRequest(aviso); 
            }
            else
            {
                newCartao.Status = "SOLICITADO";
                newCartao.Cvv = r.Next(100, 1000).ToString();
                newCartao.Numero = r.Next(1000, 9999).ToString() + " " + r.Next(1000, 9999).ToString() + " " + r.Next(1000, 9999).ToString() + " " + r.Next(1000, 9999).ToString();
                //newCartao.DataVenc = (DateTime.Today.Year + int.Parse(newCartao.DataVenc)).ToString() + "/" + DateTime.Today.Month;
                newCartao.DataVenc = (DateTime.Today.Day) +"/"+(DateTime.Today.Month.ToString().Length == 1 ? ("0" + DateTime.Today.Month) : (DateTime.Today.Month))
                + "/" + (DateTime.Today.Year + int.Parse(newCartao.DataVenc));
                await _cartoesService.CreateAsync(newCartao);
                if (!string.IsNullOrEmpty(aviso))
                    return BadRequest(aviso);

                return Ok("ID do Cartão: " + newCartao.Id + "\n" + "Número do Cartão: " + newCartao.Numero + "\n" + "Nome a ser impresso: " + newCartao.NomeCartao + "\n" + "Data de Vencimento: " + newCartao.DataVenc + "\n" 
                    + "\n\nINSTRUÇÕES PARA ATIVAÇÃO\nUtilizar o serviço 'EntregarCartão' para mudar o status de 'SOLICITADO' para 'ENTREGUE'.\nUtilizar o serviço 'AtivarCartão' e inserir os seguintes dados: número do cartão, agência, conta e senha (" + newCartao.Senha 
                    + ")\nConsulte suas informações no serviço de 'ConsultarCartão' ou 'ConsultarCartão_Full'.");
                //return CreatedAtAction(nameof(Get), new {id = newCartao.Id}, newCartao);
            }


        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, Cartao updateCartao)
        {
            var cartao = await _cartoesService.GetAsync(id);

            if (cartao is null)
            {
                return NotFound();
            }

            updateCartao.Id = cartao.Id;

            await _cartoesService.UpdateAsync(id, updateCartao);

            return NoContent();
        }

        //ATIVAÇÃO DE CARTÃO
        [HttpPut("EntregarCartao")]
        public async Task<IActionResult> EntregarCartao(string id, string numero, string agencia, string conta, string senha)
        {
            var cartao = await _cartoesService.GetAsync(id);

            cartao.Id = cartao.Id;

            if (cartao.Numero == numero && cartao.Agencia == agencia && cartao.Conta == conta && cartao.Senha == senha)
            {
                cartao.Status = "ENTREGUE";
                await _cartoesService.UpdateAsync(id, cartao);
                return Ok("Cartão Entregue!\nConsulte suas informações no serviço de 'ConsultarCartão' ou 'ConsultarCartão_Full'.");
            }
            else
            {
                return BadRequest("Cartão não encontrado. Cheque se as informações estão corretas.");
            }
        }

        //ATIVAÇÃO DE CARTÃO
        [HttpPut("AtivarCartao")]
        public async Task<IActionResult> AtivarCartao(string id, string numero, string agencia, string conta, string senha)
        {
            var cartao = await _cartoesService.GetAsyncNumero(numero);

            cartao.Id = cartao.Id;

            if (cartao.Agencia.ToString() == agencia && cartao.Conta.ToString() == conta && cartao.Senha.ToString() == senha && cartao.Numero.ToString() == numero)
            {
                if(cartao.Status == "ENTREGUE")
                {
                    System.Diagnostics.Debug.WriteLine("ATIVO");
                    cartao.Status = "ATIVO";

                    await _cartoesService.UpdateAsync(id, cartao);

                    return Ok("Cartão Ativo!\nConsulte suas informações no serviço de 'ConsultarCartão' ou 'ConsultarCartão_Full'.");
                    //return CreatedAtAction(nameof(Get), new { id = cartao.Id }, cartao);
                }
                else
                {
                    return BadRequest("É necessário que o status do cartão seja 'ENTREGUE'. Utilize o serviço de Entrega para tal.");
                }
                
            }
            else
            {
                return BadRequest("Cartão não encontrado. Cheque se as informações estão corretas.");
            }
        }

        //BLOQUEAR CARTÃO
        [HttpPut("BloquearCartao")]
        public async Task<IActionResult> BloquearCartao(string id, string numero,string agencia, string conta, string senha, string motivo)
        {
            var cartao = await _cartoesService.GetAsyncNumero(numero);

            cartao.Id = cartao.Id;

            if (cartao.Agencia.ToString() == agencia && cartao.Conta.ToString() == conta 
                && cartao.Senha.ToString() == senha && cartao.Numero == numero)
            {
                if(cartao.Status == "ATIVO")
                {
                    if (motivo == "perda" || motivo == "roubo" || motivo == "danificado")
                    {
                        cartao.Status = motivo;
                        await _cartoesService.UpdateAsync(id, cartao);
                        return Ok("Cartão Bloqueado!\nConsulte suas informações no serviço de 'ConsultarCartão' ou 'ConsultarCartão_Full'.");
                        //return CreatedAtAction(nameof(Get), new { id = cartao.Id }, cartao);
                    }
                    else
                    {
                        return BadRequest("É necessário que o motivo seja 'perda', 'roubo' ou 'danificado'.");
                    }
                }
                else
                {
                    return BadRequest("É necessário que o status do cartão seja 'ATIVO'. Ative o cartão para tal.");
                }
            }
            else
            {
                return BadRequest("Cartão não encontrado. Cheque se as informações estão corretas.");
            }

        }

        //ATIVAÇÃO DE CARTÃO
        [HttpPut("CancelarCartao")]
        public async Task<IActionResult> CancelarCartao(string id, string numero, string agencia, string conta, string senha, string textoOpcional)
        {
            var cartao = await _cartoesService.GetAsyncNumero(numero);

            cartao.Id = cartao.Id;

            if(cartao.Numero == numero && cartao.Agencia == agencia && cartao.Conta == conta && cartao.Senha == senha)
            {
                cartao.Status = "CANCELADO";
                await _cartoesService.UpdateAsync(id, cartao);
                return Ok("Cartão Cancelado!\nConsulte suas informações no serviço de 'ConsultarCartão' ou 'ConsultarCartão_Full'.");
            }
            else
            {
                return BadRequest("Cartão não encontrado. Cheque se as informações estão corretas.");
            }
        }

        [HttpGet("ConsultarCartão")]
        public async Task<ActionResult<Cartao>> ConsultarCartao(string numero)
        {
            var cartao = await _cartoesService.GetAsyncNumero(numero);

            if (cartao is null)
            {
                return BadRequest("Cartão não encontrado. Cheque se as informações estão corretas.");
            }
            else
            {
                return Ok("Número: "+cartao.Numero+".\nNome Impresso: "+cartao.NomeCartao+"\nLimite: "+cartao.Limite
                    +"\nStatus: "+cartao.Status+"\nData de Vencimento: "+cartao.DataVenc+"\nCVV: "+cartao.Cvv);
            }
        }

        [HttpGet("ConsultarCartão_Full")]
        public async Task<ActionResult<Cartao>> ConsultarCartao_Full(string numero)
        {
            var cartao = await _cartoesService.GetAsyncNumero(numero);

            if (cartao is null)
            {
                return BadRequest("Cartão não encontrado. Cheque se as informações estão corretas.");
            }

            return cartao;
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string numero)
        {
            var cartao = await _cartoesService.GetAsyncNumero(numero);

            if (cartao is null)
            {
                return NotFound();
            }

            await _cartoesService.RemoveAsync(cartao.Id!);

            return NoContent();
        }

    }
}

// Interações da interface. A única adição é a consulta de CEP via endpoint
// autenticado do próprio sistema (/Ceps/Consultar, ViaCEP). Regras de negócio,
// normalização e validação do CEP vivem no servidor — aqui só há apresentação.

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-viacep]').forEach(function (botao) {
        botao.addEventListener('click', function () {
            var url = botao.getAttribute('data-viacep-url');
            var form = botao.closest('form') || document;
            var campoCep = form.querySelector('[data-viacep-cep]');
            var mensagem = form.querySelector('[data-viacep-msg]');

            if (!url || !campoCep || !mensagem) return;

            var digitos = (campoCep.value || '').replace(/\D/g, '');

            if (digitos.length !== 8) {
                exibir(mensagem, 'CEP inválido. Confira os 8 dígitos.', false);
                campoCep.focus();
                return;
            }

            botao.disabled = true;
            exibir(mensagem, 'Consultando CEP...', true);

            fetch(url + '?cep=' + encodeURIComponent(digitos))
                .then(function (resposta) { return resposta.json(); })
                .then(function (dados) {
                    if (dados && dados.encontrado) {
                        preencher('Logradouro', dados.logradouro);
                        preencher('Bairro', dados.bairro);
                        preencher('Cidade', dados.cidade);
                        preencher('Estado', dados.estado);
                        exibir(mensagem, 'Endereço preenchido pelo CEP. Confira os números.', true);
                        var numero = document.getElementById('Numero');
                        if (numero) numero.focus();
                    } else {
                        exibir(mensagem, (dados && dados.mensagem) ||
                            'Não foi possível consultar o CEP. Preencha o endereço manualmente.', false);
                    }
                })
                .catch(function () {
                    // Falha de rede ou resposta ilegível: o formulário continua
                    // plenamente utilizável na mão — nunca bloqueia o cadastro.
                    exibir(mensagem, 'Não foi possível consultar o CEP. Preencha o endereço manualmente.', false);
                })
                .finally(function () {
                    botao.disabled = false;
                });
        });
    });

    // Só escreve no campo se ele estiver vazio ou se o valor atual veio de uma
    // consulta anterior (data-viacep-auto): dado digitado pelo usuário nunca é
    // sobrescrito.
    function preencher(id, valor) {
        var campo = document.getElementById(id);
        if (!campo || !valor) return;
        var digitadoPeloUsuario = campo.value.trim() !== '' && campo.dataset.viacepAuto !== '1';
        if (digitadoPeloUsuario) return;
        campo.value = valor;
        campo.dataset.viacepAuto = '1';
    }

    function exibir(elemento, texto, ok) {
        elemento.textContent = texto;
        elemento.classList.toggle('text-success', ok === true);
        elemento.classList.toggle('text-danger', ok !== true);
    }
});

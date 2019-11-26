using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ByteBank.View
{
    public partial class MainWindow : Window
    {
        private readonly ContaClienteRepository r_Repositorio;
        private readonly ContaClienteService r_Servico;

        public MainWindow()
        {
            InitializeComponent();

            r_Repositorio = new ContaClienteRepository();
            r_Servico = new ContaClienteService();
        }

        private void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            // Pegando o contexto da Thread principal
            var taskSchedulerUI = TaskScheduler.FromCurrentSynchronizationContext();
            BtnProcessar.IsEnabled = false;

            var contas = r_Repositorio.GetContaClientes();
            
            var resultado = new List<string>();

            AtualizarView(new List<string>(), TimeSpan.Zero);

            var inicio = DateTime.Now;
            
            // Cria uma lista de tarefas para ser executada pelo nosso gerenciador de Threads
            var contasTarefas = contas.Select(conta =>
            {
                // Task Factory é nosso gerenciador de threads, ele controla e otimiza a utilização
                // dos núcleos de processamento
                return Task.Factory.StartNew(() =>
                {

                    var resultadoConta = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(resultadoConta);
                });
            }).ToArray();

            // Quando todas as Tarefas forem finalizadas
            // esse método permite encadear Threads passando o resultado da execução de uma para outra
            Task.WhenAll(contasTarefas)
                // O método ContinueWith permite encadear Tasks, ele possui várias interfaces,
                // uma delas permite passar o contexto da TaskSchedule, nesse caso é necessário
                // pq ele está tentando acessar elemento do GUI que fica na thread principal, por
                // isso é necessário capturar ela e passar para o método saber que é para executar nesse contexto
                .ContinueWith(task => {
                    var fim = DateTime.Now;

                    // Atualiza elementos da GUI - Interface gráfica de usuário
                    AtualizarView(resultado, fim - inicio);
                }, taskSchedulerUI)
                .ContinueWith(task =>
                {
                    BtnProcessar.IsEnabled = true;
                }, taskSchedulerUI);
        }

        private void AtualizarView(List<String> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{ elapsedTime.Seconds }.{ elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count} clientes em {tempoDecorrido}";

            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }
    }
}

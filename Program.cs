using System.Text;
using LabelsTG.Labels;
using Terminal.Gui;

namespace LabelsTG
{
    class Program
    {
        static void Main(string[] args)
        {
#if NETCOREAPP
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
            Application.Init();

            bool shouldRestart;
            string configPath = args.Length > 0 ? args[0] : "conf.txt";

            do
            {
                shouldRestart = false;

                Configuration.Load(configPath);

                var top = new Toplevel();

                View view = new();
                Model model = new();
                Controller controller = new(view, model);
                //view.Controller = controller;
                //model.Controller = controller;

                controller.RestartRequested += () =>
                {
                    shouldRestart = true;
                    Application.RequestStop(); // ukončí konkrétní "top"
                };

                controller.PrintOneFileRequested += () =>
                {
                    if (model.EplFiles.Count > 0)
                    {
                        controller.PrintEplFile(model.EplFiles[0]);
                    }
                    Application.Shutdown(); // Ukončí aplikaci po vytištění
                    Environment.Exit(0); // Zajistí ukončení procesu
                };

                top.Add(view);
                Application.Run(top);

                // není nutné RemoveAll – vytvořil ses nový Toplevel
            } while (shouldRestart);

            Application.Shutdown();
        }
    }
}
